using System;
using System.Threading;
using NUnit.Framework;
using Osprey.Communication;

namespace Osprey.Tests.Communication
{
    [TestFixture]
    public class ResilientChannelTests
    {
        private class TestChannel : IChannel
        {
            public int SendCallCount { get; private set; }
            public int ReceiveCallCount { get; private set; }
            public int DisposeCallCount { get; private set; }

            public bool ShouldFailSend { get; set; }
            public bool ShouldFailReceive { get; set; }
            public int FailuresBeforeSuccess { get; set; }
            public string MessageToReturn { get; set; } = "test-message";

            public void Send(string message)
            {
                SendCallCount++;

                if (ShouldFailSend)
                {
                    if (FailuresBeforeSuccess > 0)
                    {
                        FailuresBeforeSuccess--;
                        throw new Exception("Simulated send failure");
                    }
                }
            }

            public string Receive()
            {
                ReceiveCallCount++;

                if (ShouldFailReceive)
                {
                    if (FailuresBeforeSuccess > 0)
                    {
                        FailuresBeforeSuccess--;
                        throw new Exception("Simulated receive failure");
                    }
                }

                return MessageToReturn;
            }

            public void Dispose()
            {
                DisposeCallCount++;
            }
        }

        [Test]
        public void Send_SuccessfulSend_CallsUnderlyingChannel()
        {
            // Arrange
            var testChannel = new TestChannel();
            var resilientChannel = new ResilientChannel(() => testChannel);

            // Act
            resilientChannel.Send("test");

            // Assert
            Assert.AreEqual(1, testChannel.SendCallCount);
            Assert.AreEqual(ResilientChannel.ConnectionState.Connected, resilientChannel.State);
        }

        [Test]
        public void Receive_SuccessfulReceive_ReturnsMessage()
        {
            // Arrange
            var testChannel = new TestChannel { MessageToReturn = "hello" };
            var resilientChannel = new ResilientChannel(() => testChannel);

            // Act
            var result = resilientChannel.Receive();

            // Assert
            Assert.AreEqual("hello", result);
            Assert.AreEqual(1, testChannel.ReceiveCallCount);
            Assert.AreEqual(ResilientChannel.ConnectionState.Connected, resilientChannel.State);
        }

        [Test]
        public void Send_FailureFollowedBySuccess_ReconnectsAutomatically()
        {
            // Arrange
            var creationCount = 0;
            TestChannel currentChannel = null;

            var resilientChannel = new ResilientChannel(() =>
            {
                creationCount++;
                currentChannel = new TestChannel();
                return currentChannel;
            }, new ResilientChannelConfig
            {
                InitialBackoffDelay = TimeSpan.FromMilliseconds(10),
                MaxBackoffDelay = TimeSpan.FromMilliseconds(100)
            });

            // First channel will fail once, then succeed
            var firstChannel = currentChannel;
            firstChannel.ShouldFailSend = true;
            firstChannel.FailuresBeforeSuccess = 1;

            // Act
            resilientChannel.Send("test");

            // Assert
            Assert.AreEqual(2, creationCount, "Should have created a new channel after failure");
            Assert.AreEqual(1, firstChannel.SendCallCount, "First channel should have been called once");
            Assert.Greater(currentChannel.SendCallCount, 0, "New channel should have been used");
            Assert.AreEqual(ResilientChannel.ConnectionState.Connected, resilientChannel.State);
        }

        [Test]
        public void Receive_FailureFollowedBySuccess_ReconnectsAutomatically()
        {
            // Arrange
            var creationCount = 0;
            TestChannel currentChannel = null;

            var resilientChannel = new ResilientChannel(() =>
            {
                creationCount++;
                currentChannel = new TestChannel { MessageToReturn = $"message-{creationCount}" };
                return currentChannel;
            }, new ResilientChannelConfig
            {
                InitialBackoffDelay = TimeSpan.FromMilliseconds(10),
                MaxBackoffDelay = TimeSpan.FromMilliseconds(100)
            });

            // First channel will fail once, then succeed
            var firstChannel = currentChannel;
            firstChannel.ShouldFailReceive = true;
            firstChannel.FailuresBeforeSuccess = 1;

            // Act
            var result = resilientChannel.Receive();

            // Assert
            Assert.AreEqual(2, creationCount, "Should have created a new channel after failure");
            Assert.AreEqual(1, firstChannel.ReceiveCallCount, "First channel should have been called once");
            Assert.Greater(currentChannel.ReceiveCallCount, 0, "New channel should have been used");
            Assert.AreEqual("message-2", result, "Should receive from the new channel");
            Assert.AreEqual(ResilientChannel.ConnectionState.Connected, resilientChannel.State);
        }

        [Test]
        public void Reconnection_AppliesExponentialBackoff()
        {
            // Arrange
            var config = new ResilientChannelConfig
            {
                InitialBackoffDelay = TimeSpan.FromMilliseconds(50),
                MaxBackoffDelay = TimeSpan.FromMilliseconds(200),
                BackoffMultiplier = 2.0
            };

            var attemptTimes = new System.Collections.Generic.List<DateTime>();
            var resilientChannel = new ResilientChannel(() =>
            {
                attemptTimes.Add(DateTime.UtcNow);
                var channel = new TestChannel
                {
                    ShouldFailReceive = true,
                    FailuresBeforeSuccess = attemptTimes.Count < 3 ? 1 : 0
                };
                return channel;
            }, config);

            // Act
            resilientChannel.Receive();

            // Assert
            Assert.AreEqual(3, attemptTimes.Count, "Should have made 3 attempts (initial + 2 retries)");

            // First retry should happen immediately (no backoff on first attempt)
            var delay1 = (attemptTimes[1] - attemptTimes[0]).TotalMilliseconds;
            Assert.That(delay1, Is.LessThan(30), "First retry should happen immediately");

            // Second retry should wait ~50ms (initial backoff delay)
            var delay2 = (attemptTimes[2] - attemptTimes[1]).TotalMilliseconds;
            Assert.That(delay2, Is.GreaterThanOrEqualTo(40), "Second backoff should be ~50ms");
        }

        [Test]
        public void OnReconnected_EventFired_WhenReconnectionSucceeds()
        {
            // Arrange
            var reconnectedEventFired = false;
            var creationCount = 0;

            var resilientChannel = new ResilientChannel(() =>
            {
                creationCount++;
                var channel = new TestChannel
                {
                    ShouldFailSend = creationCount == 1,
                    FailuresBeforeSuccess = 1
                };
                return channel;
            }, new ResilientChannelConfig
            {
                InitialBackoffDelay = TimeSpan.FromMilliseconds(10)
            });

            resilientChannel.OnReconnected += () => reconnectedEventFired = true;

            // Act
            resilientChannel.Send("test");

            // Assert
            Assert.IsTrue(reconnectedEventFired, "OnReconnected event should have fired");
            Assert.AreEqual(2, creationCount);
        }

        [Test]
        public void Dispose_DisconnectsAndPreventsReconnection()
        {
            // Arrange
            var testChannel = new TestChannel();
            var resilientChannel = new ResilientChannel(() => testChannel);

            // Act
            resilientChannel.Dispose();

            // Assert
            Assert.AreEqual(1, testChannel.DisposeCallCount);
            Assert.AreEqual(ResilientChannel.ConnectionState.Disconnected, resilientChannel.State);

            // Verify it throws when trying to receive after disposal
            Assert.Throws<ObjectDisposedException>(() => resilientChannel.Receive());
        }

        [Test]
        public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
        {
            // Arrange
            var testChannel = new TestChannel();
            var resilientChannel = new ResilientChannel(() => testChannel);

            // Act
            resilientChannel.Dispose();
            resilientChannel.Dispose();
            resilientChannel.Dispose();

            // Assert
            Assert.AreEqual(1, testChannel.DisposeCallCount, "Should only dispose once");
        }

        [Test]
        public void ObjectDisposedException_DuringReceive_TriggersReconnection()
        {
            // Arrange
            var creationCount = 0;
            var resilientChannel = new ResilientChannel(() =>
            {
                creationCount++;
                if (creationCount == 1)
                {
                    // First channel will throw ObjectDisposedException
                    return new DisposedChannel();
                }
                else
                {
                    // Second channel works fine
                    return new TestChannel { MessageToReturn = "recovered" };
                }
            }, new ResilientChannelConfig
            {
                InitialBackoffDelay = TimeSpan.FromMilliseconds(10)
            });

            // Act
            var result = resilientChannel.Receive();

            // Assert
            Assert.AreEqual("recovered", result);
            Assert.AreEqual(2, creationCount, "Should have recreated channel after ObjectDisposedException");
        }

        private class DisposedChannel : IChannel
        {
            public void Send(string message)
            {
                throw new ObjectDisposedException("Channel has been disposed");
            }

            public string Receive()
            {
                throw new ObjectDisposedException("Channel has been disposed");
            }

            public void Dispose()
            {
            }
        }

        [Test]
        public void MultipleFailures_RecoversEventually()
        {
            // Arrange
            var creationCount = 0;
            var resilientChannel = new ResilientChannel(() =>
            {
                creationCount++;
                return new TestChannel
                {
                    ShouldFailReceive = true,
                    FailuresBeforeSuccess = creationCount < 4 ? 1 : 0,
                    MessageToReturn = $"attempt-{creationCount}"
                };
            }, new ResilientChannelConfig
            {
                InitialBackoffDelay = TimeSpan.FromMilliseconds(10),
                MaxBackoffDelay = TimeSpan.FromMilliseconds(100)
            });

            // Act
            var result = resilientChannel.Receive();

            // Assert
            Assert.AreEqual("attempt-4", result);
            Assert.AreEqual(4, creationCount, "Should have attempted 4 times before succeeding");
            Assert.AreEqual(ResilientChannel.ConnectionState.Connected, resilientChannel.State);
        }
    }
}
