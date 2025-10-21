using System;

namespace Osprey.Communication
{
    /// <summary>
    /// Configuration for resilient channel reconnection behavior.
    /// </summary>
    public class ResilientChannelConfig
    {
        /// <summary>
        /// Initial delay between reconnection attempts.
        /// Default: 1 second.
        /// </summary>
        public TimeSpan InitialBackoffDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum delay between reconnection attempts.
        /// Default: 10 seconds.
        /// </summary>
        public TimeSpan MaxBackoffDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Multiplier applied to backoff delay after each failed attempt.
        /// Default: 2.0 (exponential backoff).
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Creates a default configuration with standard resilience settings.
        /// </summary>
        public static ResilientChannelConfig Default => new ResilientChannelConfig();
    }
}
