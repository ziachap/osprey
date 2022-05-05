using System.Collections.Generic;
using System.Linq;

namespace Osprey.Communication
{
    internal class InProcessChannel : IChannel
    {
        private readonly Queue<string> _queue;

        public InProcessChannel()
        {
            _queue = new Queue<string>();
        }
        
        public void Send(string msg)
        {
            _queue.Enqueue(msg);
        }

        public string Receive()
        {
            string msg;
            while (!_queue.TryDequeue(out msg)) { }
            return msg;
        }
    }
}
