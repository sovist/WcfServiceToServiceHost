using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using Ninject;

namespace ServiceToServiceHost
{
    /// <summary>
    /// Для сокрытия подробностей реализации от пользователя
    /// </summary>
    /// <typeparam name="TImplementedContract"></typeparam>
    /// <typeparam name="TConnectionData"></typeparam>
    internal interface IHostManagerInternalOperations<TImplementedContract, TConnectionData>
    {
        List<IConnection<TConnectionData, TImplementedContract>> Connections { get; }
        object ConnectionsSync { get; }
        void OnNewIcomingConnection(IConnection<TConnectionData, TImplementedContract> incomingConnection);
    }

    internal class HostManager<TService, TImplementedContract, TConnectionData> : 
        IHostManager<TService, TImplementedContract, TConnectionData>, 
        IHostManagerInternalOperations<TImplementedContract, TConnectionData>,
        IDisposable
        where TService : BaseService<TService, TImplementedContract, TConnectionData>, TImplementedContract
    {
        public List<IConnection<TConnectionData, TImplementedContract>> Connections { get; private set; }
        public object ConnectionsSync { get { return _connectionsSync; } }
        public IHost Host { get; private set; }
        public event Action<NewIcomingConnectionEventArgs<TConnectionData>> IcomingConnection;
        public event Action<IConnectionData<TConnectionData>> Reconnect;
        public event Action<IConnectionData<TConnectionData>> LostConnection;

        public string HostingPort
        {
            get { return _hostingPort; }
        }

        private readonly object _connectionsSync = new object();
        private readonly string _hostingPort;
        private bool _isRunning;
        private readonly string _endpointServiceName;
        private readonly Thread _hostThread;
        private IServiceBehavior _serviceBehavior;
        private readonly EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        public HostManager(string hostingPort, IKernel ninjectKernel = null)
        {            
            _hostingPort = hostingPort;
            createServiceBehavior(ninjectKernel);
            _endpointServiceName = typeof(TService).UnderlyingSystemType.Name;
            Connections = new List<IConnection<TConnectionData, TImplementedContract>>();

            _hostThread = new Thread(host);
            _hostThread.Start();
            WaitHandle.WaitAny(new WaitHandle[] {_eventWaitHandle});
        }

        private void createServiceBehavior(IKernel kernel)
        {
            if (kernel == null)
                kernel = new StandardKernel();

            var containsHost = kernel.GetBindings(typeof(IHostManager<TService, TImplementedContract, TConnectionData>));
            if (containsHost.Any())
            {
                string error = string.Format("Обнаружена пользовательская регистрация IHostManager<{0}, {1}, {2}>", typeof(TService), typeof(TImplementedContract), typeof(TConnectionData));
                error += Environment.NewLine +
                         "Не нужно регистрировать IHostManager повторно." + Environment.NewLine +
                         "Поскольку IHostManager регистрируется для решения зависимости в BaseService";
                throw new Exception(error);
            }

            kernel.Bind<IHostManager<TService, TImplementedContract, TConnectionData>>().ToMethod(_ => this);
            _serviceBehavior = new NInjectInstanceProvider(kernel);
        }

        private void host()
        {
            Host = new ServiceHost<TService, TImplementedContract, TConnectionData>(_serviceBehavior, _hostingPort, _endpointServiceName);
            _isRunning = Host.Run();
            _eventWaitHandle.Set();
        }

        public void CreateNewConnectToRemoteHost(HostAdress remoteHostAdress, IncomingOperation incomingOperation, TConnectionData connectionData)
        {
            if (!_isRunning)
            {
                L.Log.Warn("Current Host not running, CreateNewConnectToRemoteHost is Aborted");
                return;
            }

            lock (_connectionsSync)
            {
                var contains = Connections.FirstOrDefault(_ => _.Outcoming != null && _.Outcoming.Adress == remoteHostAdress);
                if (contains != null)
                {
                    contains.Data = connectionData;
                    contains.IncomingOperation = incomingOperation;
                    if (contains.Outcoming == null)
                        contains.Outcoming = createConnectionToRemoteHost(remoteHostAdress);
                    return;
                }

                Connections.Add(new Connection<TConnectionData, TImplementedContract>
                {
                    Outcoming = createConnectionToRemoteHost(remoteHostAdress),
                    Data = connectionData,
                    IncomingOperation = incomingOperation,
                    RemoteHostAdress = remoteHostAdress
                });
            }
        }

        public void RemoveConnectToRemoteHost(Predicate<IConnectionData<TConnectionData>> predicate)
        {
            lock (_connectionsSync)
            {
                var contains = Connections.FirstOrDefault(_ => predicate(_));
                if (contains == null) 
                    return;

                unsubscribeConnectionToRemoteHost(contains.Outcoming);
                Connections.Remove(contains);
                contains.Dispose();
            }
        }

        public void CallRemoteServiceMethod(Predicate<IConnectionData<TConnectionData>> predicate, Action<IOutcomingConnection<TConnectionData, TImplementedContract>> action)
        {
            List<IConnection<TConnectionData, TImplementedContract>> connections;         
            lock (_connectionsSync)
                connections = Connections.Where(_ => predicate(_)).ToList();                 
            
            if(connections.Count == 0)
                return;

            Task.Factory.StartNew(() =>
                Parallel.ForEach(connections, connection =>
                {

                    if (connection.Outcoming == null)
                        return;

                    action(connection);
                }));
        }

#region ConnectionEvents
        public void OnNewIcomingConnection(IConnection<TConnectionData, TImplementedContract> incomingConnection)
        {
            Task.Factory.StartNew(() =>
            {
                var handler = IcomingConnection;
                if (handler == null)
                    return;

                var newIcomingConnection = new NewIcomingConnectionEventArgs<TConnectionData>(incomingConnection);
                handler(newIcomingConnection);

                if (newIcomingConnection.CreateConnectionToThisRemoteHost)              
                    incomingConnection.Outcoming = createConnectionToRemoteHost(incomingConnection.RemoteHostAdress);
                
                incomingConnection.IncomingOperation = newIcomingConnection.IncomingOperation;
            });
        }

        private ConnectionToRemoteHost<TImplementedContract> createConnectionToRemoteHost(HostAdress remoteHostAdress)
        {
            var connection = new ConnectionToRemoteHost<TImplementedContract>(remoteHostAdress, _hostingPort, _endpointServiceName);
            connection.Connect.LostConnection += connectOnLostConnection;
            connection.Connect.Reconnect += connectOnReconnect;
            return connection;
        }

        private void unsubscribeConnectionToRemoteHost(IConnectionToRemoteHost<TImplementedContract> connection)
        {
            if (connection == null) 
                return;

            connection.Connect.LostConnection += connectOnLostConnection;
            connection.Connect.Reconnect += connectOnReconnect;
        }

        private IConnection<TConnectionData, TImplementedContract> getConnection(Predicate<IConnection<TConnectionData, TImplementedContract>> predicate)
        {
            lock (_connectionsSync)
                return Connections.FirstOrDefault(_ => predicate(_));
        }
        private void connectOnReconnect(IConnectionToService<TImplementedContract> connectionToService)
        {
            var connection = getConnection(_ => _.Outcoming != null && _.Outcoming.Connect == connectionToService);
            if (connection != null)
                onReconnect(connection);
        }
        private void connectOnLostConnection(IConnectionToService<TImplementedContract> connectionToService)
        {
            var connection = getConnection(_ => _.Outcoming != null && _.Outcoming.Connect == connectionToService);
            if (connection != null)
                onLostConnection(connection);
        }

        private void onReconnect(IConnectionData<TConnectionData> connectionData)
        {
            var handler = Reconnect;
            if (handler == null)
                return;

            handler(connectionData);
        }

        private void onLostConnection(IConnectionData<TConnectionData> connectionData)
        {
            var handler = LostConnection;
            if (handler == null)
                return;

            handler(connectionData);
        }
#endregion

        public void Dispose()
        {
            Host.Instance.Close();
            foreach (var connections in Connections)
            {
                try
                {
                    connections.Dispose();
                }
                catch (Exception ex)
                {
                    L.Log.Error("Disposadle error, Ex: {0}", ex);
                }
            }                          
        }
    }
}