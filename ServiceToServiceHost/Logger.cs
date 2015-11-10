using System.Diagnostics;

namespace ServiceToServiceHost
{
    public interface ILogger
    {
        void Append(string message);
    }
    public static class Logger
    {
        public static void SetLoggerInstance(ILogger logger)
        {
            L.SetLoggerInstance(logger);
        }
    }

    internal static class L
    {
        public static LoggerWriter Log { get; private set; }
    
        static L()
        {
            Log = new LoggerWriter(null);
        }

        public static void SetLoggerInstance(ILogger logger)
        {
            Log = new LoggerWriter(logger);
        }
    }

    internal class LoggerWriter
    {
        private readonly ILogger _logger;
        public LoggerWriter(ILogger logger)
        {
            _logger = logger;
        }
        public void Error(string message)
        {
            append("ERROR", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
        }
        public void Error(string format, params object[] args)
        {
            append("ERROR", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
        }
        public void Info(string message)
        {
            append("INFO", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
        }
        public void Info(string format, params object[] args)
        {
            append("INFO", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
        }
        public void Warn(string message)
        {
            append("WARN", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
        }
        public void Warn(string format, params object[] args)
        {
            append("WARN", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
        }
        public void Fatal(string message)
        {
            append("FATAL", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
        }
        public void Fatal(string format, params object[] args)
        {
            append("FATAL", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
        }
        private void append(string logError, string methodName, string logMessage)
        {
            if (_logger != null)
                _logger.Append(string.Format("{0} {1}: {2}", logError, methodName, logMessage));
        }
    }
}