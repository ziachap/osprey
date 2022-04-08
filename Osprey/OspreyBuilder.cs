using System;
using System.Collections.Generic;
using Osprey.Logging;

namespace Osprey
{
    public class OspreyBuilder
    {
        private readonly string _nodeName;
        private readonly string _environment;
        private readonly List<ServiceInfo> _services;
        
        /// <param name="nodeName">The name of the node.</param>
        /// <param name="environment">The environment to be isolated within.</param>
        public OspreyBuilder(string nodeName, string environment)
        {
            _nodeName = nodeName;
            _environment = environment;
            _services = new List<ServiceInfo>();
        }

        public OspreyBuilder SetGlobalLogger(IOspreyLogger logger)
        {
            OspreyLog.Logger = logger;
            return this;
        }

        public OspreyBuilder WithServices(params ServiceInfo[] services)
        {
            _services.AddRange(services);
            return this;
        }

        public OspreyBuilder WithService(ServiceInfo service)
        {
            _services.Add(service);
            return this;
        }
        
        /// <summary>
        /// Build an Osprey instance;
        /// </summary>
        /// <returns>Disposable osprey instance.</returns>
        public IOsprey Build()
        {
            // Force the configuration to initialize
            var _ = Configuration.Configuration.Global;

            var osprey = new Osprey(_nodeName, _environment);
            
            // Register services
            foreach (var serviceInfo in _services)
            {
                osprey.Register(serviceInfo);
            }
            
            return osprey;
        }
    }
}