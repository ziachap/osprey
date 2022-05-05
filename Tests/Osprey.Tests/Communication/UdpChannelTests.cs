using System.Net;
using NUnit.Framework;
using Osprey.Communication;

namespace Osprey.Tests.Communication
{
    public class UdpChannelTests
    {
        [Test]
        public void UdpChannel_Can_Send_Messages()
        {
            var channel = new UdpChannel(IPAddress.Loopback, IPAddress.Loopback, 5000);

            var message = "test";

            channel.Send(message);

            Assert.Pass();
        }

        [Test]
        public void UdpChannel_Can_Receive_Messages()
        {
            var channel = new UdpChannel(IPAddress.Loopback, IPAddress.Loopback, 5000);

            var message = "test";

            channel.Send(message);
            var response = channel.Receive();

            Assert.AreEqual(message, response);
        }
    }
}