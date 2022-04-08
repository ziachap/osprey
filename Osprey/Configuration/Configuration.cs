using System;
using System.IO;
using Fclp;
using Newtonsoft.Json;

namespace Osprey.Configuration
{
    public static class Configuration
    {
        private static OspreyConfiguration _global;

        public static OspreyConfiguration Global => _global ??= LoadConfiguration();

        private static OspreyConfiguration LoadConfiguration()
        {
            // Override config path from command line
            var filePath = "osprey.json";
            new FluentCommandLineParser()
                .Setup<string>("o-config")
                .Callback(value => filePath = value);

            // Load configuration
            var config = LoadJsonConfiguration(filePath);
            var args = Environment.GetCommandLineArgs();
            LoadCommandLineArguments(args, config);

            return config;
        }

        /// <summary>
        /// Loads an osprey configuration file.
        /// </summary>
        private static OspreyConfiguration LoadJsonConfiguration(string filepath)
        {
            try
            {
                var file = File.ReadAllText(filepath);
                return JsonConvert.DeserializeObject<OspreyConfiguration>(file);
            }
            catch (FileNotFoundException ex)
            {
                //_logger.Warn($"No osprey configuration file found. ({filepath})"); // TODO
                return new OspreyConfiguration();
            }
        }

        /// <summary>
        /// Overrides the configuration with any specified command line arguments 
        /// </summary>
        private static void LoadCommandLineArguments(string[] args, OspreyConfiguration config)
        {
            var p = new FluentCommandLineParser();

            //TODO: Complete these arguments
            p.Setup<string>('f', "o-udp-local-filter")
                .WithDescription("")
                .Callback(value => config.UdpBroadcastLocalFilter = value);

            p.Setup<string>('l', "o-udp-local")
                .WithDescription("")
                .Callback(value => config.UdpBroadcastLocal = value);

            p.Setup<string>('r', "o-udp-remote")
                .WithDescription("")
                .Callback(value => config.UdpBroadcastRemote = value);

            p.Setup<int>('p', "o-udp-remote-port")
                .WithDescription("")
                .Callback(value => config.UdpBroadcastPort = value);

            p.Setup<bool>('d', "o-use-dns-address")
                .WithDescription("")
                .Callback(value => config.UseDnsAddress = value);

            p.Parse(args);
        }
    }
}