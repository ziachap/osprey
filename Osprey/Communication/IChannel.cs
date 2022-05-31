using System;

namespace Osprey.Communication
{
    internal interface IChannel : IDisposable
    {
        void Send(string msg);

        string Receive();
    }
}
