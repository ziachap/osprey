using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Nancy.Bootstrapper;
using Nancy.Owin;
using Osprey.Utilities;

namespace Osprey.Http
{
    public class HttpServer<TStartup> : IDisposable where TStartup : class
    {
        private static bool _started = false;
        private readonly IWebHost _webHost;

        public HttpServer(string name)
        {
            if (_started) throw new Exception("Cannot start more than one HTTP server on a node");

            var address = Address.GenerateTcpEndpoint();

            _webHost = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<TStartup>()
                .UseUrls("http://" + address)
                .Build();

            _webHost.RunAsync();
            
            Osprey.Network.Node.Register(new ServiceInfo
            {
                Name = name,
                Address = address.ToString()
            });

            _started = true;
        }

        public void Dispose()
        {
            _webHost?.Dispose();
            _started = false;
        }
    }

    public class DefaultStartup<TBootstrapper> where TBootstrapper : INancyBootstrapper, new()
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseOwin(x => x.UseNancy(opt => opt.Bootstrapper = new TBootstrapper()));
        }
    }
}
