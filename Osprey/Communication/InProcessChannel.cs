using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Osprey.Communication
{
    internal class InProcessChannel : IChannel
    {
        private readonly BlockingCollection<string> _queue;

        public InProcessChannel()
        {
            _queue = new BlockingCollection<string>();
        }
        
        public void Send(string msg) => _queue.Add(msg);

        public string Receive() => _queue.Take();
    }
}
