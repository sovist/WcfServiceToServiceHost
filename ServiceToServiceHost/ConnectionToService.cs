using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceToServiceHost
{
    public interface IMethodCallStatus
    {
        /// <summary>
        /// Статус вызова метода, True - удачный
        /// </summary>
        bool CallStatus { get; }
    }
    public interface IMethodRezult<out TResult> : IMethodCallStatus
    {
        /// <summary>
        /// Результат что возвращает вызванный метод
        /// </summary>
        TResult Result { get; }
    }

    internal class Rezult<TResult> : IMethodRezult<TResult>
    {
        public TResult Result { get; private set; }
        public bool CallStatus { get; private set; }
        public Rezult(bool callStatus, TResult result)
        {
            Result = result;
            CallStatus = callStatus;
        }
    }
    /// <summary>
    /// Безопасно взаимодействует с сервисом
    /// </summary>
    public interface IServiceSafeMethodCall<out TIService>
    {
        /// <summary>
        /// Безопасно взаимодействует с сервисом
        /// </summary>
        /// <param name="action">метод</param>
        /// <returns>статус вызова метода</returns>
        IMethodRezult<TResult> Call<TResult>(Func<TIService, TResult> action);

        /// <summary>
        /// Безопасно взаимодействует с сервисом
        /// </summary>
        /// <param name="action">метод</param>
        /// <returns>статус вызова метода</returns>
        IMethodCallStatus Call(Action<TIService> action);
    }

    public interface IConnectionToService<out TIService> : IServiceSafeMethodCall<TIService>, IDisposable
    {
        /// <summary>
        /// Статус соединения
        /// </summary>
        bool ConnectionIsOk { get; }

        /// <summary>
        /// Происходит при удачном подключении
        /// </summary>
        event Action<IConnectionToService<TIService>> Reconnect;

        /// <summary>
        /// Происходит при каждом разрыве разрыве соединения, или при не удачной попытке подключения
        /// </summary>
        event Action<IConnectionToService<TIService>> LostConnection;
    }

    internal class ConnectionSettings
    {
        public int PingIntervalMilliseconds { get; set; }
        public int MaxTryCountCallServiceMethodIfLostConnection { get; set; }

        private int _operationTimeOutMilliseconds;
        public int OperationTimeOutMiliseconds
        {
            get { return _operationTimeOutMilliseconds + 2*PingIntervalMilliseconds; }
            set { _operationTimeOutMilliseconds = value; }
        }
    }

    internal class ConnectionToService<TIService, TIPingService> : IConnectionToService<TIService>
    {        
        private volatile bool _connectionIsOk;//состаяние подключения
        private readonly object _syncServiceCallMethod = new object();

        private readonly Action<TIPingService> _pingAction;
        private TIPingService _pingConnection;
        private TIService _serviceConnection;
        private readonly Func<ChannelFactory<TIService>> _createNewServiceChannelFactory;
        private readonly Func<ChannelFactory<TIPingService>> _createNewPingChannelFactory;
        private readonly ConnectionSettings _connectionSettings;
        private readonly Thread _connectionMonitorTask;
        private readonly string _endpointAddress;
        private bool _isDisposed;

        public event Action<IConnectionToService<TIService>> Reconnect;
        public event Action<IConnectionToService<TIService>> LostConnection;

        public bool ConnectionIsOk
        {
            get { return _connectionIsOk; }
        }

        public ConnectionToService(Func<ChannelFactory<TIService>> createNewServiceChannelFactory,
            Func<ChannelFactory<TIPingService>> createNewPingChannelFactory, Action<TIPingService> pingAction,
            ConnectionSettings connectionSettings)
        {
            _createNewServiceChannelFactory = createNewServiceChannelFactory;
            _createNewPingChannelFactory = createNewPingChannelFactory;
            _pingAction = pingAction;
            _connectionSettings = connectionSettings;

            _isDisposed = false;
            _connectionIsOk = true;

            var serviceChannelFactory = _createNewServiceChannelFactory();
            _endpointAddress = serviceChannelFactory.Endpoint.Address.Uri.Authority;

            _pingConnection = _createNewPingChannelFactory().CreateChannel();
            newConnect();

            _connectionMonitorTask = new Thread(connectionMonitorTask) {IsBackground = true};
            _connectionMonitorTask.Start();
        }

        /// <summary>
        /// пингует удаленный сервер
        /// делает Reconnect если разрыв
        /// </summary>
        private void connectionMonitorTask()
        {
            while (!_isDisposed)
            {
                try
                {
                    if (!_connectionIsOk)
                        _pingConnection = _createNewPingChannelFactory().CreateChannel();

                    _pingAction(_pingConnection);

                    if (!_connectionIsOk)
                    {
                        newConnect();
                        onReconnect();
                        _connectionIsOk = true;
                    }
                }
                catch
                {
                    if (_connectionIsOk)
                    {
                        onLostConnection();
                        _connectionIsOk = false;
                    }
                }
                Thread.Sleep(_connectionSettings.PingIntervalMilliseconds);
            }
        }

        private void newConnect()
        {
            _serviceConnection = _createNewServiceChannelFactory().CreateChannel();
            L.ExchangerLog.Info("Create new connection to {0}", _endpointAddress);
        }
        
        private void onReconnect()
        {
            var handler = Reconnect;
            if (handler == null)
                return;

            Task.Factory.StartNew(() => handler(this));
        }

        private void onLostConnection()
        {
            var handler = LostConnection;
            if (handler == null)
                return;

            Task.Factory.StartNew(() => handler(this));
        }

        public IMethodRezult<TResult> Call<TResult>(Func<TIService, TResult> action)
        {
            lock (_syncServiceCallMethod)
            {
                for (int tryCounter = 0;
                    tryCounter < _connectionSettings.MaxTryCountCallServiceMethodIfLostConnection && !_isDisposed;
                    tryCounter++)
                {
                    var startUtcTime = DateTime.UtcNow;
                    try
                    {
                        return new Rezult<TResult>(true, action(_serviceConnection));
                    }
                    catch (Exception ex)
                    {
                        L.ExchangerLog.Warn(string.Format("Try: {0}/{1}, To: {2}, ExType: {3}", tryCounter + 1,
                            _connectionSettings.MaxTryCountCallServiceMethodIfLostConnection, _endpointAddress,
                            ex.GetType()));

                        _connectionIsOk = false;
                        if ((tryCounter - _connectionSettings.MaxTryCountCallServiceMethodIfLostConnection) != 1)
                            threadSleep(startUtcTime);
                    }
                }
                return new Rezult<TResult>(false, default(TResult));
            }
        }

        private void threadSleep(DateTime startUtcTime)
        {
            var delayMilliseconds = _connectionSettings.OperationTimeOutMiliseconds - (DateTime.UtcNow - startUtcTime).Milliseconds;
            if (delayMilliseconds > 0)
                Thread.Sleep(delayMilliseconds);
        }

        public IMethodCallStatus Call(Action<TIService> action)
        {
            return Call<object>(_ =>
            {
                action(_);
                return null;
            });
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}