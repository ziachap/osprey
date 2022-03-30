using System;
using System.Threading;
using System.Threading.Tasks;
using Osprey.Communication;

namespace Osprey.ServiceDiscovery
{
    public class Broadcaster
	{
		private readonly UdpChannel _channel;
		private readonly NodeInfo _info;
        private readonly int _interval;

        internal Broadcaster(UdpChannel channel, NodeInfo info, int broadcastInterval)
		{
			_channel = channel;
			_info = info;
            _interval = broadcastInterval;
        }

		internal void Start()
		{
			Task.Factory.StartNew(() =>
			{
				while (true)
				{
                    Send(_info);
					Thread.Sleep(_interval);
				}
			}, TaskCreationOptions.LongRunning);
		}

        public void Send(NodeInfo node)
		{
			var serialized = OSPREY.Network.Serializer.Serialize(node);
            _channel.Send(serialized);
		}
	}
}