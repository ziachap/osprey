namespace Osprey.Communication
{
    internal interface IChannel
    {
        void Send(string msg);

        string Receive();
    }
}
