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

        internal Broadcaster(UdpChannel channel, NodeInfo info)
		{
			_channel = channel;
			_info = info;
        }

		internal void Start()
		{
			Task.Factory.StartNew(() =>
			{
				while (true)
				{
                    Send(_info);
					Thread.Sleep(OSPREY.Network.Config.BroadcastInterval);
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