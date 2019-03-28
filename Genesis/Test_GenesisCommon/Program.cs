using System;
using LibGenesisCommon.Common;
using LibZConfig.Common.Utils;

namespace LibGenesisCommon
{
    public class Host
    {
        public int Port { get; set; }
        public string Hostname { get; set; }
    }

    public class App
    {
        public string Name { get; set; }
        public double Version { get; set; }

        public Host Host { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            App a1 = new App();
            a1.Name = "test";
            a1.Version = 1.2;
            Host host = new Host();
            host.Port = 3322;
            a1.Host = host;

            string query = "(app.Name == \"test\" && app.Version == 1.2) || (app.Host.Port == 3322 && app.Host.Hostname == null)";
            Func<App, bool> condition = ConditionParser.Parse<App>(query, "app");
            bool ret = condition.Invoke(a1);
            LogUtils.Debug("Returned : " + ret);

            a1.Version = 2.1;
            ret = condition.Invoke(a1);
            LogUtils.Debug("Returned : " + ret);
        }
    }
}
