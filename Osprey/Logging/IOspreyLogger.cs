using System.Collections.Generic;
using System.Text;

namespace Osprey.Logging
{
    public interface IOspreyLogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
