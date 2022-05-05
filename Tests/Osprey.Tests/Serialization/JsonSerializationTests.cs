using System;
using System.Linq;
using NUnit.Framework;
using Osprey.Serialization;
using Osprey.ServiceDiscovery;
using Osprey.ServiceDiscovery.Data;

namespace Osprey.Tests.Serialization
{
    public class JsonSerializationTests
    {
        private const string TestMessage =
            "{\"id\":\"1234\",\"n\":\"test-node\",\"e\":\"testing\",\"ip\":\"100.0.0.1\",\"s\":[{\"t\":\"C\",\"n\":\"A\",\"a\":\"B\"}]}";

        [Test]
        public void JsonSerializer_Can_Serialize_Node()
        {
            var serializer = new JsonSerializer();
            var node = new NodeInfo()
            {
                Id = "1234",
                Environment = "testing",
                Ip = "100.0.0.1",
                Name = "test-node",
                Services =
                {
                    new ServiceInfo
                    {
                        Name = "A", Address = "B", Type = "C"
                    }
                }
            };

            var result = serializer.Serialize(node);

            Assert.AreEqual(result, TestMessage);
        }

        [Test]
        public void JsonSerializer_Can_Deserialize_Node()
        {
            var serializer = new JsonSerializer();

            var result = serializer.Deserialize<NodeInfo>(TestMessage);

            Assert.AreEqual(result.Id, "1234");
            Assert.AreEqual(result.Environment, "testing");
            Assert.AreEqual(result.Ip, "100.0.0.1");
            Assert.AreEqual(result.Name, "test-node");
            Assert.AreEqual(result.Services.Count, 1);
            Assert.AreEqual(result.Services.First().Name, "A");
            Assert.AreEqual(result.Services.First().Address, "B");
            Assert.AreEqual(result.Services.First().Type, "C");
        }
    }
}
