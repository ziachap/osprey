using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Osprey.Utilities;
using Osprey.ZeroMQ.Models;

namespace Osprey.ZeroMQ
{
    public class ZeroMQServer : IDisposable
    {
        /// <summary>
        /// Invoked when a client subscribes to a topic.
        /// </summary>
        public event Action<string> OnSubscribe;
        private readonly ConcurrentDictionary<string, string> _lastValueCache = new ConcurrentDictionary<string, string>();

        private bool _closed = false;

        private readonly ConcurrentDictionary<string, Client> _clients = new ConcurrentDictionary<string, Client>();
        private readonly IPEndPoint _endpoint;
        private readonly RadioSocket _relaySocket;
        private readonly IPEndPoint _relayEndpoint;
        private readonly string _node;
        private readonly string _service;

        public ZeroMQServer(string serviceName)
        {
            _node = OSPREY.Network.Node.Info.Name;
            _service = serviceName;
            _endpoint = Address.GenerateTcpEndpoint();
            _relayEndpoint = Address.GenerateUdpEndpoint();

            //StartRelayThread();
            StartListenerThread();

            var hostInfo = new ServiceInfo
            {
                Name = serviceName,
                Address = _endpoint.ToString()
            };
            
            //_relaySocket = new RadioSocket();
            //_relaySocket.Connect("udp://" + _relayEndpoint);

            OSPREY.Network.Logger.Debug("Created ZeroMQ server on " + _endpoint);

            OSPREY.Network.Node.Register(hostInfo);
        }

        private void StartRelayThread()
        {
            Task.Run(() =>
            {
                OSPREY.Network.Logger.Debug("Starting relay thread.");
                using (var server = new DishSocket())
                {
                    server.Bind("udp://" + _relayEndpoint);
                    server.Join("relay");
                    while (!_closed)
                    {
                        try
                        {
                            var msg = server.ReceiveString();
                            OSPREY.Network.Logger.Debug("Received relay");
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            OSPREY.Network.Logger.Error(ex.ToString());
                        }
                    }
                }
            });
        }

        private void StartListenerThread()
        {
            Task.Run(() =>
            {
                using (var server = new ResponseSocket())
                {
                    server.Bind("tcp://" + _endpoint);
                    while (!_closed)
                    {
                        try
                        {
                            var raw = server.ReceiveFrameString();
                            var message = OSPREY.Network.Serializer.Deserialize<EstablishRequest>(raw);

                            if (string.IsNullOrEmpty(message.ClientId))
                            {
                                OSPREY.Network.Logger.Info("Received relay: " + raw);

                                Publish(message.Topic, message.Message, false);

                                server.SendFrameEmpty();
                                continue;
                            }

                            OSPREY.Network.Logger.Debug("Received " + raw);

                            // Add the new client or replace it
                            var client = _clients.AddOrUpdate(message.ClientId, id => new Client(), (id, client) =>
                            {
                                client?.Dispose();
                                return new Client();
                            });

                            // Remove the client when it has disconnected
                            client.OnDisconnected += () =>
                            {
                                OSPREY.Network.Logger.Debug("Lost connection to client: " + message.ClientId);
                                _clients.TryRemove(message.ClientId, out _);
                            };

                            // Invoke the subscribe event when the client subscribe to a topic
                            client.OnSubscribe += topic =>
                            {
                                if (_lastValueCache.TryGetValue(topic, out var lastValue))
                                {
                                    client.Publish(topic, lastValue);
                                }

                                OnSubscribe?.Invoke(topic);
                            };

                            OSPREY.Network.Logger.Debug("Registered client: " + message.ClientId);

                            // Send back a successful response to the client
                            var response = OSPREY.Network.Serializer.Serialize(new EstablishResponse
                            {
                                ClientId = message.ClientId,
                                StreamEndpoint = client.StreamEndpoint.ToString(),
                                HeartbeatEndpoint = client.HeartbeatEndpoint.ToString(),
                                ResponseEndpoint = client.ResponseEndpoint.ToString(),
                            });
                            
                            server.SendFrame(response);

                            // Start listening to the client
                            client.StartHeartbeatThread();
                            client.StartResponseThread();
                        }
                        catch (Exception ex)
                        {
                            OSPREY.Network.Logger.Error(ex.ToString());
                            Thread.Sleep(10);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Publish data to clients that are subscribed to the given topic.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="data"></param>
        /// <param name="relay"></param>
        public void Publish(string topic, object data, bool relay = true)
        {
            var msg = OSPREY.Network.Serializer.Serialize(data);

            _lastValueCache[topic] = msg;

            foreach (var client in _clients.Values)
            {
                client.Publish(topic, msg);
            }

            if (relay)
            {
                var others = OSPREY.Network.LocateEnvironment(_node).Where(x => x.Id != OSPREY.Network.Node.Info.Id).ToList();

                if (!others.Any()) return;

                var relayMsg = new EstablishRequest
                {
                    Topic = topic,
                    Message = msg
                };

                var json = OSPREY.Network.Serializer.Serialize(relayMsg);

                foreach (var other in others)
                {
                    var service = other.Services.First(x => x.Name == _service);
                    using (var client = new RequestSocket())
                    {
                        client.Connect("tcp://" + service.Address);
                        if (!client.TrySendFrame(TimeSpan.FromSeconds(3), json))
                        {
                            throw new TimeoutException("Timed out while waiting to send relay to server.");
                        };
                        if (!client.TryReceiveFrameString(TimeSpan.FromSeconds(3), out _))
                        {
                            throw new TimeoutException("Timed out while waiting to receive relay resp from server.");
                        };
                    }
                }
            }
        }

        public void Dispose()
        {
            _closed = true;
        }
    }

    public class Client : IDisposable
    {
        private const int HeartbeatTimeoutMs = 3000;
        private const int HeartbeatIntervalMs = 1000;

        public event Action OnDisconnected;
        public event Action<string> OnSubscribe;

        private bool _closed = false;
        public readonly HashSet<string> Topics = new HashSet<string>();

        public Client()
        {
            OnDisconnected += Dispose;

            StreamSocket = new PublisherSocket();
            StreamEndpoint = Address.GenerateTcpEndpoint();
            StreamSocket.Options.SendHighWatermark = 1000;
            StreamSocket.Bind("tcp://" + StreamEndpoint);

            HeartbeatSocket = new RequestSocket();
            HeartbeatEndpoint = Address.GenerateTcpEndpoint();
            HeartbeatSocket.Bind("tcp://" + HeartbeatEndpoint);

            ResponseSocket = new ResponseSocket();
            ResponseEndpoint = Address.GenerateTcpEndpoint();
            ResponseSocket.Bind("tcp://" + ResponseEndpoint);
        }

        public IPEndPoint StreamEndpoint { get; }
        public IPEndPoint HeartbeatEndpoint { get; }
        public IPEndPoint ResponseEndpoint { get; }
        
        public PublisherSocket StreamSocket { get; }
        public RequestSocket HeartbeatSocket { get; }
        public ResponseSocket ResponseSocket { get; }

        public void StartHeartbeatThread()
        {
            Task.Run(() =>
            {
                while (!_closed)
                {
                    if (!HeartbeatSocket.TrySendFrame(TimeSpan.FromMilliseconds(HeartbeatTimeoutMs), "ping"))
                    {
                        OSPREY.Network.Logger.Warn("Sending heartbeat failed.");
                        OnDisconnected?.Invoke();
                        return;
                    }
                    if (!HeartbeatSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(HeartbeatTimeoutMs), out var response))
                    {
                        OSPREY.Network.Logger.Warn("Receiving heartbeat response failed.");
                        OnDisconnected?.Invoke();
                        return;
                    }
                    Thread.Sleep(HeartbeatIntervalMs);
                }
            }).ContinueWith(task =>
            {
                OSPREY.Network.Logger.Warn("¬ Heartbeat sending thread has ended.");
            });
        }

        public void StartResponseThread()
        {
            Task.Run(() =>
            {
                OSPREY.Network.Logger.Debug("Started request/response thread.");
                while (!_closed)
                {
                    var command = ResponseSocket.ReceiveFrameString();
                    var msg = ResponseSocket.ReceiveFrameString();

                    if (command == "subscribe")
                    {
                        Topics.Add(msg);

                        try
                        {
                            OnSubscribe?.Invoke(msg);
                        }
                        catch (Exception ex)
                        {
                            // TODO
                            if (!ResponseSocket.TrySendFrame(TimeSpan.FromMilliseconds(HeartbeatTimeoutMs), ex.Message))
                            {
                                OSPREY.Network.Logger.Warn("Sending error response failed.");
                                OnDisconnected?.Invoke();
                            }
                            return;
                        }
                    }
                    else if (command == "unsubscribe")
                    {
                        Topics.Remove(msg);
                    }
                    
                    if (!ResponseSocket.TrySendFrame(TimeSpan.FromMilliseconds(HeartbeatTimeoutMs), "OK"))
                    {
                        OSPREY.Network.Logger.Warn("Sending response failed.");
                        OnDisconnected?.Invoke();
                        return;
                    }
                }
            }).ContinueWith(task =>
            {
                OSPREY.Network.Logger.Warn("¬ Request/response thread has ended.");
            });
        }

        private readonly object _streamLock = new object();
        public void Publish(string topic, string data)
        {
            if (!Topics.Contains(topic)) return;

            lock (_streamLock)
            {
                if (!StreamSocket.TrySendFrame(TimeSpan.FromSeconds(1), topic, true))
                {
                    throw new TimeoutException("Timed out while publishing data topic.");
                }
                if (!StreamSocket.TrySendFrame(TimeSpan.FromSeconds(1), data))
                {
                    throw new TimeoutException("Timed out while publishing data message.");
                }
            }
        }
        
        public void Dispose()
        {
            _closed = true;
            StreamSocket?.Dispose();
        }
    }
}
