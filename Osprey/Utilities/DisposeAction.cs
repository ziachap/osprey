using System;

namespace Osprey.Utilities
{
    internal class DisposeAction : IDisposable
    {
        private readonly Action _dispose;

        public DisposeAction(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
        }
    }
}