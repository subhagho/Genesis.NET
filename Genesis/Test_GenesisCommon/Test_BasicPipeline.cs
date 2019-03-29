using System;
using Xunit;
using LibZConfig.Common.Config;
using LibZConfig.Common.Utils;
using LibZConfig.Common.Config.Readers;
using LibZConfig.Common.Config.Nodes;
using LibZConfig.Common.Config.Parsers;
using Version = LibZConfig.Common.Config.Version;
using LibGenesisCommon.Process;

namespace LibGenesisCommon.Tests
{
    public class BasicPipeline
    {
        private const string CONFIG_BASIC_PROPS_FILE = @"../../../Resources/test-pipeline.properties";
        private const string CONFIG_PROP_NAME = "config.name";
        private const string CONFIG_PROP_FILENAME = "config.file";
        private const string CONFIG_PROP_VERSION = "config.version";

        private static Configuration configuration;

        private Configuration GetConfiguration()
        {
            if (configuration == null)
            {
                try
                {
                    Properties properties = new Properties();
                    properties.Load(CONFIG_BASIC_PROPS_FILE);

                    string cname = properties.GetProperty(CONFIG_PROP_NAME);
                    Assert.False(String.IsNullOrWhiteSpace(cname));
                    string cfile = properties.GetProperty(CONFIG_PROP_FILENAME);
                    Assert.False(String.IsNullOrWhiteSpace(cfile));
                    string version = properties.GetProperty(CONFIG_PROP_VERSION);
                    Assert.False(String.IsNullOrWhiteSpace(version));

                    LogUtils.Info(String.Format("Reading Configuration: [file={0}][version={1}]", cfile, version));

                    using (FileReader reader = new FileReader(cfile))
                    {
                        reader.Open();
                        XmlConfigParser parser = new XmlConfigParser();
                        ConfigurationSettings settings = new ConfigurationSettings();
                        settings.DownloadOptions = EDownloadOptions.LoadRemoteResourcesOnStartup;

                        parser.Parse(cname, reader, Version.Parse(version), settings);

                        configuration = parser.GetConfiguration();

                        return configuration;
                    }
                }
                catch (Exception ex)
                {
                    LogUtils.Error(ex);
                    throw ex;
                }
            }
            return configuration;
        }

        [Fact]
        public void LoadPipeline()
        {
            try
            {
                Configuration config = GetConfiguration();
                Assert.NotNull(config);

                PipelineBuilder builder = new PipelineBuilder();
                builder.Load(config.RootConfigNode);
                
            }
            catch (Exception ex)
            {
                LogUtils.Error(ex);
                throw ex;
            }
        }
    }
}