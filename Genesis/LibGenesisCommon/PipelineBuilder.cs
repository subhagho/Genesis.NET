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
    [ConfigPath()]
    public class PipelineDef
    {
        [ConfigAttribute(Name = "name", Required = true)]
        public string Name { get; set; }
        [ConfigAttribute(Name = "type", Required = true)]
        public string Type { get; set; }
        [ConfigAttribute(Name = "assembly", Required = false)]
        public string Assembly { get; set; }
    }

    [ConfigPath()]
    public class ProcessDef
    {
        [ConfigAttribute(Name = "name", Required = true)]
        public string Name { get; set; }
        [ConfigAttribute(Name = "type", Required = true)]
        public string Type { get; set; }
        [ConfigAttribute(Name = "assembly", Required = false)]
        public string Assembly { get; set; }
        [ConfigValue(Name = "condition", Required = false)]
        public string Condition { get; set; }
    }

    public class PipelineBuilder
    {
        public const string CONFIG_NODE_PIPELINES = "pipelines";
        public const string CONFIG_NODE_PIPELINE = "pipeline";
        public const string CONFIG_NODE_PROCESSORS = "processors";
        public const string CONFIG_NODE_PROCESSOR = "processor";

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
            if (config.Name != CONFIG_NODE_PIPELINES)
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
                    LoadPipeline((ConfigPathNode)ps);
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
            PipelineDef def = ConfigurationAnnotationProcessor.Process<PipelineDef>(node);
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
            PropertyInfo pi = TypeUtils.FindProperty(type, "Name");
            if (pi == null)
            {
                throw new ProcessException(String.Format("Property not found: 'Name' [type={0}]", type.FullName));
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
                foreach(ConfigElementNode elem in nodes.GetValues())
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
            LogUtils.Debug(String.Format("Loading processor from node. [node={0}]", node.GetAbsolutePath()));
            if (node.Name != CONFIG_NODE_PROCESSOR)
            {
                LogUtils.Warn(String.Format("Invalid Processor Node: [path={0}]", node.GetSearchPath()));
                return;
            }
            ProcessDef def = ConfigurationAnnotationProcessor.Process<ProcessDef>(node);
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
                throw new ProcessException(String.Format("Error creating processor instance. [type={0}]", type.FullName));
            }
            if (!ReflectionUtils.IsSubclassOfRawGeneric(obj.GetType(), typeof(Processor<>)))
            {
                throw new ProcessException(String.Format("Invalid processor type. [type={0}]", type.FullName));
            }
            PropertyInfo pi = TypeUtils.FindProperty(type, "Name");
            if (pi == null)
            {
                throw new ProcessException(String.Format("Property not found: 'Name' [type={0}]", type.FullName));
            }
            pi.SetValue(obj, def.Name);
        }
    }
}
