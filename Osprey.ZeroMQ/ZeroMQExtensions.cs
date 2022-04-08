using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

namespace Osprey.ZeroMQ
{
    // TODO: Don't think we're gonna be using any of these anymore
    public static class ZeroMQExtensions
    {
        public static string ReceiveString(this NetworkStream stream, CancellationToken ct)
        {
            var lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, lengthBytes.Length);
            var length = BitConverter.ToInt32(lengthBytes);

            var bytes = new byte[length];
            stream.Read(bytes, 0, length);
            var str = Encoding.ASCII.GetString(bytes);

            return str;
        }
        
        public static void Send(this ServiceInfo service, string data)
        {
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://" + service.Address);
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine("Sending " + data);
                    client.SendFrame(data);
                    var message = client.ReceiveFrameString();
                    Console.WriteLine("Received {0}", message);
                }
            }
        }

        public static void Send<T>(this Socket socket, T data)
        {
            var bytes = data switch
            {
                string s => Encoding.ASCII.GetBytes(s),
                _ => Encoding.ASCII.GetBytes(Osprey.Network.Serializer.Serialize(data))
            };

            var length = BitConverter.GetBytes(bytes.Length);
            if (length.Length != 4) throw new Exception("Message length should be 4 bytes");

            var message = length.Concat(bytes).ToArray();

            socket.Send(message);
        }

        public static IPEndPoint AsIpEndpoint(this string ip) => IPEndPoint.Parse(ip);

        /*
        public static IMessage DecodeMessage(this string message)
        {
            var type = message.FromJson<Message>().Type;

            return type switch
            {
                "add" => message.FromJson<AddMessage>(),
                "edit" => message.FromJson<EditMessage>(),
                "delete" => message.FromJson<DeleteMessage>(),
                "heartbeat" => message.FromJson<HeartbeatMessage>(),
                "refresh" => message.FromJson<RefreshMessage>(),
                _ => message.FromJson<Message>()
            };
        }*/
    }
}