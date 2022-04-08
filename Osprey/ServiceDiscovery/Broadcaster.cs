using System;
using System.Threading;
using System.Threading.Tasks;
using Osprey.Communication;
using Osprey.Configuration;

namespace Osprey.ServiceDiscovery
{
    public class Broadcaster
    {
		private readonly UdpChannel _channel;
        private readonly IOsprey _osprey;

		internal Broadcaster(UdpChannel channel, IOsprey osprey)
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
                    Send(_osprey.Info);
					Thread.Sleep(Configuration.Configuration.Global.BroadcastInterval);
				}
			}, TaskCreationOptions.LongRunning);
		}

        public void Send(NodeInfo node)
		{
			var serialized = _osprey.Serializer.Serialize(node);
            _channel.Send(serialized);
		}
	}
}