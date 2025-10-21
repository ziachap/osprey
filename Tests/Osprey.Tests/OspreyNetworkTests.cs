using System;
using FluentAssertions;
using NUnit.Framework;
using Osprey.Communication;
using Osprey.Configuration;

namespace Osprey.Tests
{
    public class OspreyNetworkTests
    {
        [Test]
        public void OspreyNetwork_Uses_InProcessChannel_When_DisableNetworking_Is_True()
        {
            Config.Global.DisableUdpNetworking = true;
            using (var network = new OspreyNetwork("test", "test_env"))
            {
                network._broadcastChannel.Should().BeOfType<InProcessChannel>();
            }
        }

        [Test]
        public void OspreyNetwork_Uses_ResilientChannel_When_DisableNetworking_Is_False()
        {
            Config.Global.DisableUdpNetworking = false;
            using (var network = new OspreyNetwork("test", "test_env"))
            {
                // UDP channel is now wrapped in a ResilientChannel for automatic reconnection
                network._broadcastChannel.Should().BeOfType<ResilientChannel>();
            }
        }
    }
}
