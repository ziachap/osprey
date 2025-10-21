using System;
using System.Threading;
using System.Threading.Tasks;
using Osprey.Communication;
using Osprey.Configuration;
using Osprey.Logging;
using Osprey.ServiceDiscovery.Data;

namespace Osprey.ServiceDiscovery
{
    public class Broadcaster
    {
		private readonly IChannel _channel;
        private readonly IOsprey _osprey;

		internal Broadcaster(IChannel channel, IOsprey osprey)
		{
			_channel = channel;
            _osprey = osprey;
        }

		internal void Start()
		{
			Task.Factory.StartNew(() =>
			{
				while (true)
				{
                    Send(_osprey.Node);
					Thread.Sleep(Config.Global.BroadcastInterval);
				}
			}, TaskCreationOptions.LongRunning);
		}

        public void Send(NodeInfo node)
		{
			try
			{
				var serialized = _osprey.Serializer.Serialize(node);
				_channel.Send(serialized);
			}
			catch (Exception ex)
			{
				// Unexpected error during broadcast
				OspreyLog.Warn("Failed to broadcast service discovery message.");
				OspreyLog.Error(ex.ToString());
				// Continue trying on next broadcast interval
			}
		}
	}
}