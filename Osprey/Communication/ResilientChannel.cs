using System;
using System.Threading;
using Osprey.Logging;

namespace Osprey.Communication
{
    /// <summary>
    /// A resilient channel wrapper that automatically reconnects on failure.
    /// </summary>
    internal class ResilientChannel : IChannel
    {
        private readonly Func<IChannel> _channelFactory;
        private readonly ResilientChannelConfig _config;
        private readonly object _lock = new object();

        private IChannel _channel;
        private bool _disposed = false;
        private int _reconnectionAttempts = 0;
        private TimeSpan _currentBackoffDelay;

        /// <summary>
        /// Current connection state of the channel.
        /// </summary>
        public enum ConnectionState
        {
            Connected,
            Disconnected,
            Reconnecting
        }

        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        public ConnectionState State { get; private set; } = ConnectionState.Connected;

        /// <summary>
        /// Event raised when the channel successfully reconnects.
        /// </summary>
        public event Action OnReconnected;

        /// <summary>
        /// Event raised when the channel disconnects.
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// Creates a new resilient channel wrapper.
        /// </summary>
        /// <param name="channelFactory">Factory function to create new channel instances.</param>
        /// <param name="config">Configuration for reconnection behavior.</param>
        public ResilientChannel(Func<IChannel> channelFactory, ResilientChannelConfig config = null)
        {
            _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _config = config ?? ResilientChannelConfig.Default;
            _currentBackoffDelay = _config.InitialBackoffDelay;

            _channel = _channelFactory();
        }

        /// <summary>
        /// Sends a message through the channel, reconnecting if necessary.
        /// </summary>
        public void Send(string message)
        {
            while (!_disposed)
            {
                lock (_lock)
                {
                    if (_disposed) return;

                    try
                    {
                        _channel.Send(message);
                        return; // Success
                    }
                    catch (ObjectDisposedException)
                    {
                        if (_disposed) return;
                        OspreyLog.Warn("Channel disposed during send, attempting reconnection.");
                        TryReconnect(); // Will retry forever until success or disposal
                        // If we get here, either reconnected successfully or disposed
                        if (_disposed) return;
                    }
                    catch (Exception ex)
                    {
                        OspreyLog.Warn($"Failed to send message: {ex.Message}");
                        TryReconnect(); // Will retry forever until success or disposal
                        // If we get here, either reconnected successfully or disposed
                        if (_disposed) return;
                    }
                }
            }
        }

        /// <summary>
        /// Receives a message from the channel, reconnecting if necessary.
        /// </summary>
        public string Receive()
        {
            while (!_disposed)
            {
                lock (_lock)
                {
                    if (_disposed) throw new ObjectDisposedException(nameof(ResilientChannel));

                    try
                    {
                        var message = _channel.Receive();

                        // Reset reconnection state on successful receive
                        if (_reconnectionAttempts > 0)
                        {
                            OspreyLog.Info("Channel fully recovered after reconnection.");
                            _reconnectionAttempts = 0;
                            _currentBackoffDelay = _config.InitialBackoffDelay;
                        }

                        return message;
                    }
                    catch (ObjectDisposedException)
                    {
                        if (_disposed) throw;
                        OspreyLog.Warn("Channel disposed during receive, attempting reconnection.");
                        TryReconnect(); // Will retry forever until success or disposal
                        // If we get here, either reconnected successfully or disposed
                        if (_disposed) throw new ObjectDisposedException(nameof(ResilientChannel));
                    }
                    catch (Exception ex)
                    {
                        if (_disposed) throw;
                        OspreyLog.Debug($"Failed to receive message: {ex.Message}");
                        TryReconnect(); // Will retry forever until success or disposal
                        // If we get here, either reconnected successfully or disposed
                        if (_disposed) throw new ObjectDisposedException(nameof(ResilientChannel));
                    }
                }
            }

            throw new ObjectDisposedException(nameof(ResilientChannel));
        }

        /// <summary>
        /// Attempts to reconnect the channel with exponential backoff.
        /// Keeps trying forever until success or disposal.
        /// </summary>
        private void TryReconnect()
        {
            State = ConnectionState.Reconnecting;

            // Keep trying forever until success or disposal
            while (!_disposed)
            {
                _reconnectionAttempts++;

                OspreyLog.Info($"Attempting to reconnect channel (attempt {_reconnectionAttempts})...");

                // Apply backoff delay (but not on first attempt)
                if (_reconnectionAttempts > 1)
                {
                    OspreyLog.Debug($"Waiting {_currentBackoffDelay.TotalSeconds:F1}s before reconnection attempt...");
                    Thread.Sleep(_currentBackoffDelay);

                    // Increase backoff delay for next attempt
                    _currentBackoffDelay = TimeSpan.FromMilliseconds(
                        Math.Min(_currentBackoffDelay.TotalMilliseconds * _config.BackoffMultiplier,
                                 _config.MaxBackoffDelay.TotalMilliseconds)
                    );
                }

                if (_disposed) return; // Check after sleep

                try
                {
                    // Dispose old channel
                    try
                    {
                        _channel?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        OspreyLog.Debug($"Error disposing old channel: {ex.Message}");
                    }

                    // Create new channel
                    _channel = _channelFactory();
                    State = ConnectionState.Connected;

                    OspreyLog.Info("Channel reconnected successfully.");
                    OnReconnected?.Invoke();

                    return; // Success!
                }
                catch (Exception ex)
                {
                    OspreyLog.Warn($"Reconnection attempt {_reconnectionAttempts} failed: {ex.Message}");
                    // Continue loop to retry
                }
            }
        }

        /// <summary>
        /// Disposes the channel and prevents reconnection.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;

                _disposed = true;
                State = ConnectionState.Disconnected;

                try
                {
                    _channel?.Dispose();
                }
                catch (Exception ex)
                {
                    OspreyLog.Debug($"Error disposing channel: {ex.Message}");
                }
            }
        }
    }
}
