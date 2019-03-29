#region copyright
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
// Copyright (c) 2019
// Date: 2019-3-28
// Project: LibGenesisCommon
// Subho Ghosh (subho dot ghosh at outlook.com)
//
//
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using LibZConfig.Common.Utils;
using LibZConfig.Common.Config;
using LibZConfig.Common.Config.Attributes;
using LibZConfig.Common.Config.Nodes;

namespace LibGenesisCommon.Process
{
    [ConfigPath(Path = "pipeline")]
    public class PipelineConfig
    {
        [ConfigAttribute(Name = "name", Required = true)]
        public string Name { get; set; }
        [ConfigAttribute(Name = "type", Required = true)]
        public string Type { get; set; }
        [ConfigAttribute(Name = "assembly", Required = false)]
        public string Assembly { get; set; }
    }

    [ConfigPath(Path = "processor")]
    public class ProcessConfig
    {
        [ConfigAttribute(Name = "name", Required = true)]
        public string Name { get; set; }
        [ConfigAttribute(Name = "type", Required = true)]
        public string Type { get; set; }
        [ConfigAttribute(Name = "assembly", Required = false)]
        public string Assembly { get; set; }
        [ConfigAttribute(Name = "reference", Required = false)]
        public bool IsReference { get; set; }
        [ConfigAttribute(Path ="condition", Name = "clause", Required = false)]
        public string Condition { get; set; }
        [ConfigAttribute(Path = "condition", Name = "typeName", Required = false)]
        public string TypeName { get; set; }
    }

    public class PipelineBuilder
    {
        public const string CONFIG_NODE_PIPELINES = "pipelines";
        public const string CONFIG_NODE_PIPELINE = "pipeline";
        public const string CONFIG_NODE_PROCESSORS = "processors";
        public const string CONFIG_NODE_PROCESSOR = "processor";
        public const string PROPPERTY_NAME = "Name";
        public const string METHOD_ADD = "Add";
        
        private Dictionary<string, object> pipelines = new Dictionary<string, object>();

        public Pipeline<T> GetPipeline<T>(string name)
        {
            if (pipelines.ContainsKey(name))
            {
                object obj = pipelines[name];
                if (obj != null)
                {
                    if (ReflectionUtils.ImplementsGenericInterface(obj.GetType(), typeof(Pipeline<>)))
                    {
                        return (Pipeline<T>)obj;
                    }
                }
            }
            return null;
        }

        public void Load(AbstractConfigNode config)
        {
            Contract.Requires(config != null);
            AbstractConfigNode ps = null;
            if (config.Name == CONFIG_NODE_PIPELINES)
            {
                ps = config;
            }
            else
            {
                ps = config.Find(CONFIG_NODE_PIPELINES);
            }
            if (ps != null)
            {
                if (ps.GetType() == typeof(ConfigPathNode))
                {
                    ps = ps.Find(CONFIG_NODE_PIPELINE);
                    if (ps != null && ps.GetType() == typeof(ConfigPathNode))
                    {
                        LoadPipeline((ConfigPathNode)ps);
                    }
                    else
                    {
                        LogUtils.Warn(String.Format("No pipeline defined: [path={0}]", config.GetAbsolutePath()));
                    }
                }
                else if (ps.GetType() == typeof(ConfigElementListNode))
                {
                    ConfigElementListNode nodes = (ConfigElementListNode)ps;
                    foreach (ConfigElementNode elem in nodes.GetValues())
                    {
                        if (elem.GetType() == typeof(ConfigPathNode) && elem.Name == CONFIG_NODE_PIPELINE)
                        {
                            LoadPipeline((ConfigPathNode)elem);
                        }
                    }
                }
            }
        }

        private void LoadPipeline(ConfigPathNode node)
        {
            LogUtils.Debug(String.Format("Loading pipeline from node. [node={0}]", node.GetAbsolutePath()));
            if (node.Name != CONFIG_NODE_PIPELINE)
            {
                LogUtils.Warn(String.Format("Invalid Pipeline Node: [path={0}]", node.GetSearchPath()));
                return;
            }
            PipelineConfig def = ConfigurationAnnotationProcessor.Process<PipelineConfig>(node);
            if (def == null)
            {
                throw new ProcessException(String.Format("Error reading pipeline definition. [path={0}]", node.GetAbsolutePath()));
            }
            string typename = def.Type;
            if (!String.IsNullOrWhiteSpace(def.Assembly))
            {
                typename = String.Format("{0}, {1}", def.Type, def.Assembly);
            }
            Type type = Type.GetType(typename);
            object obj = ConfigurationAnnotationProcessor.CreateInstance(type, node);
            if (obj == null)
            {
                throw new ProcessException(String.Format("Error creating pipeline instance. [type={0}]", type.FullName));
            }
            if (!ReflectionUtils.ImplementsGenericInterface(obj.GetType(), typeof(Pipeline<>)))
            {
                throw new ProcessException(String.Format("Invalid pipeline type. [type={0}]", type.FullName));
            }
            PropertyInfo pi = TypeUtils.FindProperty(type, PROPPERTY_NAME);
            if (pi == null)
            {
                throw new ProcessException(String.Format("Property not found: [property={0}][type={1}]", PROPPERTY_NAME, type.FullName));
            }
            pi.SetValue(obj, def.Name);

            LoadProcessors(obj, node, def.Name);

            if (pipelines.ContainsKey(def.Name))
            {
                throw new ProcessException(String.Format("Duplicate pipeline name: [name={0}][type={1}]", def.Name, type.FullName));
            }
            pipelines[def.Name] = obj;
        }

        private void LoadProcessors(object pipeline, ConfigPathNode node, string name)
        {
            AbstractConfigNode pnode = node.Find(CONFIG_NODE_PROCESSORS);
            if (pnode == null)
            {
                throw new ProcessException(String.Format("Pipeline has no processors defined: [name={0}][type={1}]", name, pipeline.GetType().FullName));
            }
            if (pnode.GetType() == typeof(ConfigPathNode))
            {
                LoadProcessor(pipeline, (ConfigPathNode)pnode, name);
            }
            else if (pnode.GetType() == typeof(ConfigElementListNode))
            {
                ConfigElementListNode nodes = (ConfigElementListNode)pnode;
                foreach (ConfigElementNode elem in nodes.GetValues())
                {
                    if (elem.GetType() == typeof(ConfigPathNode) && elem.Name == CONFIG_NODE_PROCESSOR)
                    {
                        LoadProcessor(pipeline, (ConfigPathNode)elem, name);
                    }
                }
            }
        }

        private void LoadProcessor(object pipeline, ConfigPathNode node, string pname)
        {
            LogUtils.Debug(String.Format("Loading processor from node. [pipeline={0}][node={1}]", pname, node.GetAbsolutePath()));
            if (node.Name != CONFIG_NODE_PROCESSOR)
            {
                LogUtils.Warn(String.Format("Invalid Processor Node: [path={0}]", node.GetSearchPath()));
                return;
            }
            ProcessConfig def = ConfigurationAnnotationProcessor.Process<ProcessConfig>(node);
            if (def == null)
            {
                throw new ProcessException(String.Format("Error reading pipeline definition. [path={0}]", node.GetAbsolutePath()));
            }
            string typename = def.Type;
            if (!String.IsNullOrWhiteSpace(def.Assembly))
            {
                typename = String.Format("{0}, {1}", def.Type, def.Assembly);
            }
            Type type = Type.GetType(typename);
            object obj = null;
            if (def.IsReference)
            {
                if (ReflectionUtils.ImplementsGenericInterface(type, typeof(Pipeline<>)))
                {
                    if (pipelines.ContainsKey(def.Name))
                    {
                        obj = pipelines[def.Name];
                    }
                    else
                    {
                        throw new ProcessException(String.Format("Referenced Pipeline not found: [name={0}][type={1}]", def.Name, type.FullName));
                    }
                }
            }
            else
            {
                obj = ConfigurationAnnotationProcessor.CreateInstance(type, node);
                if (obj == null)
                {
                    throw new ProcessException(String.Format("Error creating processor instance. [type={0}]", type.FullName));
                }
                if (!ReflectionUtils.IsSubclassOfRawGeneric(obj.GetType(), typeof(Processor<>)))
                {
                    throw new ProcessException(String.Format("Invalid processor type. [type={0}]", type.FullName));
                }
                PropertyInfo pi = TypeUtils.FindProperty(type, "Name");
                if (pi == null)
                {
                    throw new ProcessException(String.Format("Property not found: [property={0}][type={1}]", PROPPERTY_NAME, type.FullName));
                }
                pi.SetValue(obj, def.Name);
            }
            AddProcessor(pipeline, obj, def.Condition, def.TypeName);
        }

        private void AddProcessor(object pipeline, object processor, string condition, string prefix)
        {
            MethodInfo mi = pipeline.GetType().GetMethod(METHOD_ADD);
            if (mi == null)
            {
                throw new ProcessException(String.Format("Method not found: [method={0}][type={1}]", METHOD_ADD, pipeline.GetType().FullName));
            }
            mi.Invoke(pipeline, new[] { processor, condition, prefix });
        }
    }
}
