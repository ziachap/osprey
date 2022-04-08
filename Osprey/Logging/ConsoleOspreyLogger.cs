using System;

namespace Osprey.Logging
{
    public class ConsoleOspreyLogger : IOspreyLogger
    {
        public void Trace(string message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | TRACE | " + message);

        public void Debug(string message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | DEBUG | " + message);

        public void Info(string message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | INFO  | " + message);

        public void Warn(string message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | WARN  | " + message);

        public void Error(string message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ERROR | " + message);
    }
}