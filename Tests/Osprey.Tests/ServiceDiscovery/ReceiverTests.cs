using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Osprey.Communication;
using Osprey.Configuration;
using Osprey.Serialization;
using Osprey.ServiceDiscovery;
using Osprey.ServiceDiscovery.Data;

namespace Osprey.Tests.ServiceDiscovery
{ 
    public class ReceiverTests
    {
        [Test]
        public void Active_Shows_Service()
        {
            var channel = new InProcessChannel();
            var receiver = new Receiver(channel, Serializer);
            receiver.Start();

            BroadcastNode(channel, new NodeInfo()
            {
                Id = "1234",
                Name = "node1",
                Environment = "testing"
            });
            
            Thread.Sleep(300);

            var active = receiver.Active;

            active.Should().HaveCount(1);
        }

        [Test]
        public void Services_Are_Removed_From_Active_After_Timeout()
        {
            Config.Global.DiscoveryTimeout = 1;

            var channel = new InProcessChannel();
            var receiver = new Receiver(channel, Serializer);
            receiver.Start();

            BroadcastNode(channel, new NodeInfo()
            {
                Id = "1234",
                Name = "node1",
                Environment = "testing"
            });
            
            Thread.Sleep(300);

            var active = receiver.Active;

            active.Should().HaveCount(0);
        }

        [Test]
        public void Locate_Filters_By_Node_Name()
        {
            var channel = new InProcessChannel();
            var receiver = new Receiver(channel, Serializer);
            receiver.Start();

            BroadcastNode(channel, new NodeInfo()
            {
                Id = "1234",
                Name = "node1",
                Environment = "testing"
            });

            BroadcastNode(channel, new NodeInfo()
            {
                Id = "5678",
                Name = "node2",
                Environment = "testing"
            });

            Thread.Sleep(300);

            var result = receiver.Locate("node1", "testing", true);

            result.Id.Should().Be("1234");
        }

        [Test]
        public void Locate_Filters_By_Environment()
        {
            var channel = new InProcessChannel();
            var receiver = new Receiver(channel, Serializer);
            receiver.Start();

            BroadcastNode(channel, new NodeInfo()
            {
                Id = "1234",
                Name = "node1",
                Environment = "testing1"
            });

            BroadcastNode(channel, new NodeInfo()
            {
                Id = "5678",
                Name = "node1",
                Environment = "testing2"
            });

            Thread.Sleep(300);

            var result = receiver.Locate("node1", "testing2", true);

            result.Id.Should().Be("5678");
        }

        private void BroadcastNode(IChannel channel, NodeInfo node)
        {
            var msg = Serializer.Serialize(node);
            channel.Send(msg);
        }

        private ISerializer Serializer => new JsonSerializer();
    }
}
