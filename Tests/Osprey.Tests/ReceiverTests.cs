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

namespace Osprey.Tests
{
    public class OspreyNetworkTests
    {
        [Test]
        public void OspreyNetwork_Uses_InProcessChannel_When_DisableNetworking_Is_True()
        {
            using (var network = new OspreyNetwork("test", "test_env"))
            {
                Config.Global.DisableUdpNetworking = true;
                network._broadcastChannel.Should().BeOfType<InProcessChannel>();
            }
        }

        [Test]
        public void OspreyNetwork_Uses_UdpChannel_When_DisableNetworking_Is_False()
        {
            using (var network = new OspreyNetwork("test", "test_env"))
            {
                Config.Global.DisableUdpNetworking = false;
                network._broadcastChannel.Should().BeOfType<InProcessChannel>();
            }
        }
    }
}
