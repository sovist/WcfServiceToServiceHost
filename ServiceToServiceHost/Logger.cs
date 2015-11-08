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
            if (_logger == null) 
                return;

            message = string.Format("Error {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
            _logger.Append(message);
        }
        public void Error(string format, params object[] args)
        {
            if (_logger == null)
                return;

            format = string.Format("Info {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
            _logger.Append(format);
        }

        public void Info(string message)
        {
            if (_logger == null)
                return;

            message = string.Format("Info {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
            _logger.Append(message);
        }
        public void Info(string format, params object[] args)
        {
            if (_logger == null)
                return;

            format = string.Format("Info {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
            _logger.Append(format);
        }

        public void Warn(string message)
        {
            if (_logger == null)
                return;

            message = string.Format("Warn {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
            _logger.Append(message);
        }
        public void Warn(string format, params object[] args)
        {
            if (_logger == null)
                return;

            format = string.Format("Info {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
            _logger.Append(format);
        }

        public void Fatal(string message)
        {
            if (_logger == null)
                return;

            message = string.Format("Fatal {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, message);
            _logger.Append(message);
        }
        public void Fatal(string format, params object[] args)
        {
            if (_logger == null)
                return;

            format = string.Format("Info {0}: {1}", (new StackTrace()).GetFrame(1).GetMethod().Name, string.Format(null, format, args));
            _logger.Append(format);
        }
    }
}