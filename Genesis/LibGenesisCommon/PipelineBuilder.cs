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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using LibZConfig.Common.Utils;
using LibZConfig.Common.Config;
using LibZConfig.Common.Config.Attributes;
using LibZConfig.Common.Config.Nodes;

namespace LibGenesisCommon.Process
{
    /// <summary>
    /// Config class to auto-wire defined pipeline configurations.
    /// </summary>
    [ConfigPath(Path = "pipeline")]
    public class PipelineConfig
    {
        /// <summary>
        /// Pipeline name - Must be unique for the builder context.
        /// </summary>
        [ConfigAttribute(Name = "name", Required = true)]
        public string Name { get; set; }
        /// <summary>
        /// Pipeline class - Class extending Base/Collection pipeline. 
        /// Note: Generics are not supported at this time.
        /// </summary>
        [ConfigAttribute(Name = "type", Required = true)]
        public string Type { get; set; }
        /// <summary>
        /// Assembly to load the pipeline type from.
        /// </summary>
        [ConfigAttribute(Name = "assembly", Required = true)]
        public string Assembly { get; set; }
    }

    [ConfigPath(Path = "processor")]
    public class ProcessConfig
    {
        /// <summary>
        /// Processor name - Must be unqiue in the context of a pipeline
        /// </summary>
        [ConfigAttribute(Name = "name", Required = true)]
        public string Name { get; set; }
        /// <summary>
        /// Processor class - Class extending Base/Collection processor. 
        /// Note: Generics are not supported at this time.
        /// </summary>
        [ConfigAttribute(Name = "type", Required = true)]
        public string Type { get; set; }
        /// <summary>
        /// Assembly to load the processor type from.
        /// </summary>
        [ConfigAttribute(Name = "assembly", Required = true)]
        public string Assembly { get; set; }
        /// <summary>
        /// Pipeline definition reference - If referring to an already defined
        /// pipeline.
        /// Pipelines can be embedded as processors in other pipelines.
        /// </summary>
        [ConfigAttribute(Name = "reference", Required = false)]
        public bool IsReference { get; set; }
        /// <summary>
        /// Condition definition - Linq Clause used to decide if the current processor
        /// should operate on the data.
        /// </summary>
        [ConfigAttribute(Path = "condition", Name = "clause", Required = false)]
        public string Condition { get; set; }
        /// <summary>
        /// Entity prefix used in defining the condition. Required if condition is defined.
        /// </summary>
        [ConfigAttribute(Path = "condition", Name = "typeName", Required = false)]
        public string TypeName { get; set; }
    }

    /// <summary>
    /// Builder class to load defined pipelines.
    /// </summary>
    public class PipelineBuilder
    {
        /// <summary>
        /// Pipelines List definition tag
        /// </summary>
        public const string CONFIG_NODE_PIPELINES = "pipelines";
        /// <summary>
        /// Pipeline definition tag
        /// </summary>
        public const string CONFIG_NODE_PIPELINE = "pipeline";
        /// <summary>
        /// Processors List definition tag
        /// </summary>
        public const string CONFIG_NODE_PROCESSORS = "processors";
        /// <summary>
        /// Processor definition tag
        /// </summary>
        public const string CONFIG_NODE_PROCESSOR = "processor";
        /// <summary>
        /// Processor Property: Name
        /// </summary>
        public const string PROPPERTY_NAME = "Name";
        /// <summary>
        /// Pipeline Method: Add
        /// </summary>
        public const string METHOD_ADD = "Add";

        private Dictionary<string, object> pipelines = new Dictionary<string, object>();

        /// <summary>
        /// Get a defined pipeline by the specified name.
        /// </summary>
        /// <typeparam name="T">Data Type</typeparam>
        /// <param name="name">Pipeline name</param>
        /// <returns>Pipeline Instance</returns>
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

        /// <summary>
        /// Load defined pipelines from the configuration.
        /// </summary>
        /// <param name="config">Configuration Node</param>
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

        /// <summary>
        /// Load a pipeline definition from the specified node.
        /// </summary>
        /// <param name="node">Configuration Node</param>
        private void LoadPipeline(ConfigPathNode node)
        {
            LogUtils.Debug(String.Format("Loading pipeline from node. [node={0}]", node.GetAbsolutePath()));
            if (node.Name != CONFIG_NODE_PIPELINE)
            {
                LogUtils.Warn(String.Format("Invalid Pipeline Node: [path={0}]", node.GetSearchPath()));
                return;
            }
            PipelineConfig def = ConfigurationAnnotationProcessor.Process<PipelineConfig>(node);
            Conditions.NotNull(def);
            Type type = null;
            string typename = def.Type;
            if (!String.IsNullOrWhiteSpace(def.Assembly))
            {
                string aname = Path.GetFileName(def.Assembly);
                Assembly asm = AssemblyUtils.GetOrLoadAssembly(aname, def.Assembly);
                Conditions.NotNull(asm);
                type = asm.GetType(typename, true);
                Conditions.NotNull(type);
            }
            else
            {
                type = Type.GetType(typename, true);
                Conditions.NotNull(type);
            }
            object obj = ConfigurationAnnotationProcessor.CreateInstance(type, node);
            Conditions.NotNull(obj);
            if (!ReflectionUtils.ImplementsGenericInterface(obj.GetType(), typeof(Pipeline<>)))
            {
                throw new ProcessException(String.Format("Invalid pipeline type. [type={0}]", type.FullName));
            }
            PropertyInfo pi = TypeUtils.FindProperty(type, PROPPERTY_NAME);
            Conditions.NotNull(pi);
            pi.SetValue(obj, def.Name);

            LoadProcessors(obj, node, def.Name);

            if (pipelines.ContainsKey(def.Name))
            {
                throw new ProcessException(String.Format("Duplicate pipeline name: [name={0}][type={1}]", def.Name, type.FullName));
            }
            pipelines[def.Name] = obj;
        }

        /// <summary>
        /// Load the defined processes for the pipeline.
        /// </summary>
        /// <param name="pipeline">Parent pipeline</param>
        /// <param name="node">Configuration node.</param>
        /// <param name="name">Pipeline name</param>
        private void LoadProcessors(object pipeline, ConfigPathNode node, string name)
        {
            AbstractConfigNode pnode = node.Find(CONFIG_NODE_PROCESSORS);
            Conditions.NotNull(pnode);
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

        /// <summary>
        /// Load a defined process from the configuration node.
        /// </summary>
        /// <param name="pipeline">Parent pipeline</param>
        /// <param name="node">Configuration node</param>
        /// <param name="pname">Pipeline name</param>
        private void LoadProcessor(object pipeline, ConfigPathNode node, string pname)
        {
            LogUtils.Debug(String.Format("Loading processor from node. [pipeline={0}][node={1}]", pname, node.GetAbsolutePath()));
            if (node.Name != CONFIG_NODE_PROCESSOR)
            {
                LogUtils.Warn(String.Format("Invalid Processor Node: [path={0}]", node.GetSearchPath()));
                return;
            }
            ProcessConfig def = ConfigurationAnnotationProcessor.Process<ProcessConfig>(node);
            Conditions.NotNull(def);
            string typename = def.Type;
            Type type = null;
            if (!String.IsNullOrWhiteSpace(def.Assembly))
            {
                string aname = Path.GetFileName(def.Assembly);
                Assembly asm = AssemblyUtils.GetOrLoadAssembly(aname, def.Assembly);
                Conditions.NotNull(asm);
                type = asm.GetType(typename, true);
                Conditions.NotNull(type);
            }
            else
            {
                type = Type.GetType(typename, true);
                Conditions.NotNull(type);
            }
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
                Conditions.NotNull(obj);
                if (!ReflectionUtils.IsSubclassOfRawGeneric(obj.GetType(), typeof(Processor<>)))
                {
                    throw new ProcessException(String.Format("Invalid processor type. [type={0}]", type.FullName));
                }
                PropertyInfo pi = TypeUtils.FindProperty(type, "Name");
                Conditions.NotNull(pi);
                pi.SetValue(obj, def.Name);
            }
            AddProcessor(pipeline, obj, def.Condition, def.TypeName);
        }

        /// <summary>
        /// Add the processor instance to the pipeline.
        /// </summary>
        /// <param name="pipeline">Parent pipeline</param>
        /// <param name="processor">Processor instance.</param>
        /// <param name="condition">Condition (Linq Clause) if defined.</param>
        /// <param name="prefix">Condition data prefix.</param>
        private void AddProcessor(object pipeline, object processor, string condition, string prefix)
        {
            MethodInfo mi = pipeline.GetType().GetMethod(METHOD_ADD);
            Conditions.NotNull(mi);
            mi.Invoke(pipeline, new[] { processor, condition, prefix });
        }
    }
}
