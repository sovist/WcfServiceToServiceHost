using System;
using System.ServiceModel;
using System.Threading;

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
        /// Происходит при каждом разрыве разрыве соединения, или при не удачной попытке подключения
        /// </summary>
        event Action<IConnectionToService<TIService>> LostConnection;

        /// <summary>
        /// происходит при удачном подключении или пере подключении
        /// </summary>
        event Action<IConnectionToService<TIService>> Connected;

        /// <summary>
        /// подключится
        /// </summary>
        void Connect();
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
        private readonly string _endpointAddress;
        private Thread _connectionMonitorTask;
        private bool _isDisposed;
  
        /// <summary>
        /// происходит при потере соединения
        /// </summary>
        public event Action<IConnectionToService<TIService>> LostConnection;
        /// <summary>
        /// происходит при первом удачном соединении
        /// </summary>
        public event Action<IConnectionToService<TIService>> Connected;

        public bool ConnectionIsOk => _connectionIsOk;

        public ConnectionToService(Func<ChannelFactory<TIService>> createNewServiceChannelFactory,
            Func<ChannelFactory<TIPingService>> createNewPingChannelFactory, Action<TIPingService> pingAction,
            ConnectionSettings connectionSettings)
        {
            _createNewServiceChannelFactory = createNewServiceChannelFactory;
            _createNewPingChannelFactory = createNewPingChannelFactory;
            _pingAction = pingAction;
            _connectionSettings = connectionSettings;
            _isDisposed = false;

            var serviceChannelFactory = _createNewServiceChannelFactory();
            _endpointAddress = serviceChannelFactory.Endpoint.Address.Uri.Authority;

            newConnect();
        }

        public void Connect()
        {
            if(_connectionMonitorTask != null)
                return;

            _connectionMonitorTask = new Thread(connectionMonitorTask) { IsBackground = true };
            _connectionMonitorTask.Start();
        }

        /// <summary>
        /// пингует удаленный сервер
        /// делает Reconnect если разрыв
        /// </summary>
        private void connectionMonitorTask()
        {            
            var pingConnectionIsOk = false;
            while (!_isDisposed)
            {
                try
                {
                    if (!pingConnectionIsOk)
                        _pingConnection = _createNewPingChannelFactory().CreateChannel();

                    _pingAction(_pingConnection);

                    if (!pingConnectionIsOk)
                    {
                        newConnect(DateTime.UtcNow);
                        onConnected();
                        pingConnectionIsOk = true;
                    }
                }
                catch
                {
                    if (pingConnectionIsOk)
                    {
                        onLostConnection();
                        pingConnectionIsOk = false;
                    }                                
                }
                Thread.Sleep(_connectionSettings.PingIntervalMilliseconds);
            }
        }

        private void newConnect(DateTime utcNow)
        {
            var interval = getOperationIntervalMillisecondsIfLostConnection(utcNow);
            if (interval > 0)
            {
                L.Log.Info("getOperationIntervalMillisecondsIfLostConnection {0}", interval);
                return;
            }

            newConnect();
        }
        private void newConnect()
        {
            _serviceConnection = _createNewServiceChannelFactory().CreateChannel();
            L.Log.Info("Create new connection to {0}", _endpointAddress);
        }

        private void onConnected()    
        {
            Connected?.Invoke(this);
        }
        private void onLostConnection()
        {
            LostConnection?.Invoke(this);
        }

        public IMethodRezult<TResult> Call<TResult>(Func<TIService, TResult> action)
        {
            lock (_syncServiceCallMethod)
            {
                for (var tryCounter = 0;
                    tryCounter < _connectionSettings.MaxTryCountCallServiceMethodIfLostConnection && !_isDisposed;
                    tryCounter++)
                {
                    var startUtcTime = DateTime.UtcNow;
                    try
                    {
                        var res = new Rezult<TResult>(true, action(_serviceConnection));
                        _connectionIsOk = true;
                        return res;
                    }
                    catch (Exception ex)
                    {
                        _connectionIsOk = false;
                        
                        L.Log.Warn("Try: {0}/{1}, To: {2}, ExType: {3}", tryCounter + 1, _connectionSettings.MaxTryCountCallServiceMethodIfLostConnection, _endpointAddress, ex.GetType());

                        if ((tryCounter - _connectionSettings.MaxTryCountCallServiceMethodIfLostConnection) != 1)
                            delay(startUtcTime);

                        newConnect(DateTime.UtcNow);
                    }
                }
                return new Rezult<TResult>(false, default(TResult));
            }
        }

        private void delay(DateTime startUtcTime)
        {
            var delayMilliseconds = getOperationIntervalMillisecondsIfLostConnection(startUtcTime);
            if (delayMilliseconds > 0)
                Thread.Sleep(delayMilliseconds);
        }

        private int getOperationIntervalMillisecondsIfLostConnection(DateTime utcTime)
        {
            return (DateTime.UtcNow - utcTime.AddMilliseconds(_connectionSettings.OperationTimeOutMiliseconds)).Milliseconds;
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