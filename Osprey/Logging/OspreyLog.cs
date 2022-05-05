namespace Osprey.Logging
{
    /// <summary>
    /// Wraps around a global instance of a logger used for internal logging within Osprey.
    /// </summary>
    public static class OspreyLog
    {
        public static IOspreyLogger Logger { private get; set; } = new ConsoleOspreyLogger();

        public static void Trace(string msg) => Logger.Trace(msg);

        public static void Debug(string msg) => Logger.Debug(msg);

        public static void Info(string msg) => Logger.Info(msg);

        public static void Warn(string msg) => Logger.Warn(msg);

        public static void Error(string msg) => Logger.Error(msg);
    }
}