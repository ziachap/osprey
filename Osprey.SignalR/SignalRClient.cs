using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Osprey.Logging;
using Osprey.ServiceDiscovery;
using Osprey.Utilities;
using Exception = System.Exception;

namespace Osprey.SignalR
{
    /// <summary>
    /// Wrapper around HubConnection that locates a SignalR service on an Osprey network.
    /// </summary>
    public interface IOspreySignalRClient : IDisposable
    {
        event Func<Exception, Task> Disconnected;
        event Func<string, Task> Connected;

        /// <summary>
        /// Connect to the SignalR hub. The client will continually attempt to
        /// reconnect after a disconnection until disposed.
        /// </summary>
        /// <returns></returns>
        Task StartConnection();

        /// <summary>
        /// Registers a handler that will be invoked when the hub method with the
        /// specified method name is invoked.
        /// </summary>
        void On<T>(string method, Action<T> handler);

        /// <summary>
        /// Invokes a hub method on the server using the specified method name and arguments.
        /// </summary>
        Task InvokeCoreAsync(string method, params object[] args);

        /// <summary>
        /// Invokes a hub method on the server using the specified method name and arguments.
        /// </summary>
        Task InvokeCoreAsync(string method, object[] args, CancellationToken cancellationToken);
    }
    
    public class OspreySignalRClient : IOspreySignalRClient
    {
        private readonly IOsprey _network;
        private readonly string _node;
        private readonly string _service;
        private readonly string _hubEndpointUrl;
        private readonly int _retryMilliseconds;
        private readonly Action<IHubConnectionBuilder> _builder;
        private readonly List<Action<HubConnection>> _rebuildActions = new List<Action<HubConnection>>();

        private HubConnection _connection;
        private bool _disposed;

        public string Environment { get; set; } = null;
        
        public event Func<Exception, Task> Disconnected;
        public event Func<string, Task> Connected;
        
        public OspreySignalRClient(IOsprey osprey, string node, string service, string hubEndpointUrl, int retryMilliseconds = 3000, Action<IHubConnectionBuilder> builder = null)
        {
            _network = osprey;
            _node = node;
            _service = service;
            _hubEndpointUrl = hubEndpointUrl;
            _retryMilliseconds = retryMilliseconds;
            _builder = builder ?? (b => {});
        }

        public async Task StartConnection()
        {
            var connected = false;

            while (!connected && !_disposed)
            {
                try
                {
                    if (_connection != null)
                    {
                        await _connection.DisposeAsync();
                        _connection = null;
                    }

                    var url = _network
                        .Locate(_node, Environment, true)
                        .FindService(_service, true)
                        .Address;

                    var builder = new HubConnectionBuilder().WithUrl("http://" + url + _hubEndpointUrl);

                    _builder(builder);

                    _connection = builder.Build();

                    foreach (var rebuildAction in _rebuildActions)
                    {
                        rebuildAction?.Invoke(_connection);
                    }

                    _connection.Closed += async ex =>
                    {
                        if (_disposed) return;
                        if (Disconnected != null) await Disconnected.Invoke(ex);
                        await StartConnection();
                    };
                    
                    await _connection.StartAsync();

                    connected = true;
                    Connected?.Invoke("Connected to hub.");
                }
                catch (ServiceUnavailableException ex)
                {
                    OspreyLog.Warn(ex.ToString());
                    await Task.Delay(_retryMilliseconds);
                }
                catch (Exception ex)
                {
                    OspreyLog.Warn(ex.ToString());
                    await Task.Delay(_retryMilliseconds);
                }
            }
        }
        
        public void On<T>(string method, Action<T> handler)
        {
            _connection?.On(method, handler);
            _rebuildActions.Add(connection => connection.On(method, handler));
        }

        public Task InvokeCoreAsync(string method, params object[] args)
        {
            return _connection.InvokeCoreAsync(method, args);
        }

        public Task InvokeCoreAsync(string method, object[] args, CancellationToken cancellationToken)
        {
            return _connection.InvokeCoreAsync(method, args, cancellationToken);
        }

        public void Dispose()
        {
            _disposed = true;
            _connection?.DisposeAsync();
        }
    }
}
