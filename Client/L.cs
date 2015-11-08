using System.Diagnostics;
using NLog;

namespace Client
{
    internal static class L
    {
        public static LoggerWithCallMemberName AppLog { get; private set; }

        static L()
        {
             AppLog = (LoggerWithCallMemberName)LogManager.GetLogger("ExchangerServiceLog", typeof(LoggerWithCallMemberName));
        }
    }
    /// <summary>
    /// Додає в лог назву методу що його викликав
    /// Не завжди вірновідображає метод що викливав лог, наприклад при foreach виводить MoveNext
    /// Томущо відображає лише попередній фрейм стеку. 
    /// </summary>
    public class LoggerWithCallMemberName : NLog.Logger
    {
        public new void Error(string message)
        {
            base.Error((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message);
        }
        public new void Error(string message, params object[] args)
        {
            base.Error((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message, args);
        }
        public new void Info(string message)
        {
            base.Info((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message);
        }
        public new void Info(string message, params object[] args)
        {
            base.Info((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message, args);
        }
        public new void Warn(string message)
        {
            base.Warn((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message);
        }
        public new void Warn(string message, params object[] args)
        {
            base.Warn((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message, args);
        }
        public new void Fatal(string message)
        {
            base.Fatal((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message);
        }
        public new void Fatal(string message, params object[] args)
        {
            base.Fatal((new StackTrace()).GetFrame(1).GetMethod().Name + ": " + message, args);
        }
    }
}
