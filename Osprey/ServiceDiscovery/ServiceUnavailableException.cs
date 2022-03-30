using System;

namespace Osprey.ServiceDiscovery
{
    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException()
        {
            
        }

        public ServiceUnavailableException(string message) : base(message)
        {
            
        }
    }
}