using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Osprey.Logging;

namespace Osprey.Communication
{
    internal class UdpChannel : IDisposable
    {
        private readonly UdpClient _client;
        private readonly IPEndPoint _local;
        private readonly IPEndPoint _remote;

        public string LocalEndpoint => _local.ToString();
        public string RemoteEndpoint => _remote.ToString();

        public UdpChannel(IPAddress remote, IPAddress local, int port)
        {
            _remote = new IPEndPoint(remote, port);
            _local = new IPEndPoint(local, port);

            _client = new UdpClient();
            _client.EnableBroadcast = true;
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 3);
            _client.Client.ReceiveTimeout = 30000;
            _client.Client.Bind(_local);

            if (IsMulticast(remote))
            {
                OspreyLog.Debug("Joining multicast group.");
                _client.JoinMulticastGroup(remote, local);
            }
        }

        public void Send(string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            _client.Send(bytes, bytes.Length, _remote);
        }

        public string Receive()
        {
            var _ = new IPEndPoint(IPAddress.Any, 0);
            var buffer = _client.Receive(ref _);
            var message = Encoding.UTF8.GetString(buffer);
            //OspreyLog.Trace(message);
            return message;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private static bool IsMulticast(IPAddress address)
        {
            OspreyLog.Debug("Address remote: " + address);
            OspreyLog.Debug("Address bytes remote [0]: " + address.GetAddressBytes()[0]);
            return address.GetAddressBytes()[0] >= 224 && address.GetAddressBytes()[0] <= 239;
        }
    }
}
