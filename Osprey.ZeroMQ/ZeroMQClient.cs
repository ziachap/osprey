using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Osprey.ZeroMQ.Models;

namespace Osprey.ZeroMQ
{
    public class ZeroMQClient : IDisposable
    {
        private const int HeartbeatTimeoutMs = 3000;

        public event Action OnConnected;
        public event Action OnDisconnected;
        
        private readonly string _id;
        private readonly ConcurrentDictionary<string, ConcurrentBag<SubscriptionHandler>> _subscriptions
            = new ConcurrentDictionary<string, ConcurrentBag<SubscriptionHandler>>();

        private string _nodeName;
        private string _serviceName;

        private ResponseSocket _heartbeatSocket;
        private SubscriberSocket _streamSocket;
        private RequestSocket _requestSocket;
        private bool _connected = false;
        
        public ZeroMQClient(string node, string service)
        {
            _id = Guid.NewGuid().ToString();
            _nodeName = node;
            _serviceName = service;
            OnDisconnected += () =>
            {
                _connected = false;
                _streamSocket?.Dispose();
                Task.Run(RetryConnect);
            };
        }

        private void RetryConnect()
        {
            while (!_connected)
            {
                try
                {
                    Connect();
                    return;
                }
                catch (Exception ex)
                {
                    Osprey.Network.Logger.Warn("Failed to reconnect: " + ex.Message);
                }
                Thread.Sleep(2000);
            }
        }

        public void Connect()
        {
            // Locate service
            var expires = DateTime.Now.AddSeconds(3);
            ServiceInfo serviceInfo = null;
            while (serviceInfo == null)
            {
                serviceInfo = Osprey.Network.Locate(_nodeName)?.Services.First(x => x.Name == _serviceName);
                Thread.Sleep(10);

                if (DateTime.Now > expires)
                    throw new TimeoutException("Timed out while trying to locate service.");
            } 

            // Establish connection to server
            var data = new EstablishRequest
            {
                ClientId = Osprey.Network.Node.Info.Id
            };

            var json = Osprey.Network.Serializer.Serialize(data);

            string streamEndpoint;
            string heartbeatEndpoint;
            string requestEndpoint;
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://" + serviceInfo.Address);
                if (!client.TrySendFrame(TimeSpan.FromSeconds(3), json))
                {
                    throw new TimeoutException("Timed out while waiting to send to server.");
                };

                if (!client.TryReceiveFrameString(TimeSpan.FromSeconds(3), out var raw))
                {
                    throw new TimeoutException("Timed out while waiting for response from server.");
                };

                var response = Osprey.Network.Serializer.Deserialize<EstablishResponse>(raw);

                Osprey.Network.Logger.Debug("Stream address is: " + response.StreamEndpoint);
                Osprey.Network.Logger.Debug("Heartbeat address is: " + response.HeartbeatEndpoint);

                streamEndpoint = response.StreamEndpoint;
                heartbeatEndpoint = response.HeartbeatEndpoint;
                requestEndpoint = response.ResponseEndpoint;
            }

            // Setup streaming socket
            _streamSocket = new SubscriberSocket();
            _streamSocket.Options.ReceiveHighWatermark = 1000;
            _streamSocket.Connect("tcp://" + streamEndpoint);
            _streamSocket.SubscribeToAnyTopic();

            // Setup heartbeat socket
            _heartbeatSocket = new ResponseSocket();
            _heartbeatSocket.Connect("tcp://" + heartbeatEndpoint);

            _requestSocket = new RequestSocket();
            _requestSocket.Connect("tcp://" + requestEndpoint);

            _connected = true;
            StartHeartbeatThread();
            StartListenerThread();

            OnConnected?.Invoke();
        }

        private void StartHeartbeatThread()
        {
            Task.Run(() =>
            {
                Osprey.Network.Logger.Debug("Heartbeat started for " + _id);
                try
                {
                    while (_connected)
                    {
                        if (!_heartbeatSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(HeartbeatTimeoutMs), out var request))
                            throw new TimeoutException("Heartbeat timed out");
                        if (!_heartbeatSocket.TrySendFrame(TimeSpan.FromMilliseconds(HeartbeatTimeoutMs), "pong"))
                            throw new TimeoutException("Heartbeat timed out");
                    }
                }
                catch (TimeoutException ex)
                {
                    OnDisconnected?.Invoke();
                }
            }).ContinueWith(task =>
            {
                Osprey.Network.Logger.Debug("¬ Heartbeat thread has ended.");
            });
        }

        private void StartListenerThread()
        {
            Task.Run(() =>
            {
                Osprey.Network.Logger.Debug("Listener started for " + _id);
                Osprey.Network.Logger.Debug("Streaming socket is listening");

                while (_connected)
                {
                    string topic;
                    while (!_streamSocket.TryReceiveFrameString(out topic))
                    {
                        if (!_connected) return;
                        Thread.Sleep(1);
                    }
                    
                    var handlers = _subscriptions.GetOrAdd(topic, topic => new ConcurrentBag<SubscriptionHandler>());

                    string msg;
                    while (!_streamSocket.TryReceiveFrameString(out msg))
                    {
                        if (!_connected) return;
                        Thread.Sleep(1);
                    }

                    foreach (var handler in handlers)
                    {
                        var deserialized = Osprey.Network.Serializer.Deserialize(msg, handler.DeserializeType);
                        handler.Handler?.Invoke(deserialized);
                    }

                   //Console.WriteLine($"{DateTime.Now} | {topic} = {msg}");
                }
            }).ContinueWith(task =>
            {
                Osprey.Network.Logger.Debug("¬ Listener thread has ended.");
            });
        }
        
        public void Subscribe(string topic)
        {
            Console.WriteLine("Subscribing to: " + topic);

            if (!_requestSocket.TrySendFrame(TimeSpan.FromMilliseconds(3000), "subscribe", more: true))
                throw new TimeoutException("Sending request command timed out");
            if (!_requestSocket.TrySendFrame(TimeSpan.FromMilliseconds(3000), topic))
                throw new TimeoutException("Sending request parameter timed out");
            if (!_requestSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(7000), out _))
                throw new TimeoutException("Waiting for response timed out");

            Osprey.Network.Logger.Debug("Subscribed to: " + topic);
        }
        
        public void Unsubscribe(string topic)
        {
            Osprey.Network.Logger.Debug("Unsubscribing from: " + topic);

            if (!_requestSocket.TrySendFrame(TimeSpan.FromMilliseconds(3000), "unsubscribe", more: true))
                throw new TimeoutException("Sending request command timed out");
            if (!_requestSocket.TrySendFrame(TimeSpan.FromMilliseconds(3000), topic))
                throw new TimeoutException("Sending request parameter timed out");
            if (!_requestSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(7000), out _))
                throw new TimeoutException("Waiting for response timed out");

            Osprey.Network.Logger.Debug("Unsubscribed from: " + topic);
        }

        public void On<T>(string topic, Action<T> action) where T : class
        {
            var handler = new Action<object>(data =>
            {
                var casted = data as T ?? throw new Exception("Cannot cast data to target type: " + typeof(T).Name);
                action?.Invoke(casted);
            });

            _subscriptions.AddOrUpdate(topic, new ConcurrentBag<SubscriptionHandler>()
            {
                new SubscriptionHandler(typeof(T), handler)
            }, (t, h) =>
            {
                h.Add(new SubscriptionHandler(typeof(T), handler));
                return h;
            });
        }

        public void Dispose()
        {
            _streamSocket?.Dispose();
        }

        private class SubscriptionHandler
        {
            public SubscriptionHandler(Type deserializeType, Action<object> handler)
            {
                DeserializeType = deserializeType;
                Handler = handler;
            }

            public Type DeserializeType { get; }
            public Action<object> Handler { get; }
        }
    }

}
