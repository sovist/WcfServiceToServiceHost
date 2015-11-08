using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace ServiceToServiceHost
{
    [ServiceContract]
    internal interface IBaseService
    {

        [OperationContract]
        bool Ping();
    }

    /// <summary>
    /// ������� ����� �������
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public abstract class BaseService<TService, TImplementedContract, TConnectionData> : IBaseService
    {
        private readonly IHostManagerInternalOperations<TImplementedContract, TConnectionData> _hostManagerInternalOperations;
        private readonly string _fromIp;

        protected readonly IHostManager<TService, TImplementedContract, TConnectionData> HostManager;
        protected IConnection<TConnectionData, TImplementedContract> CurrentConnection;
        protected OperationContext CurrentOperationContext;

        protected BaseService(IHostManager<TService, TImplementedContract, TConnectionData> hostManager)
        {
            CurrentOperationContext = OperationContext.Current;
            HostManager = hostManager;

            _hostManagerInternalOperations = hostManager as IHostManagerInternalOperations<TImplementedContract, TConnectionData>;
            if (_hostManagerInternalOperations == null)            
                throw new Exception("����� ������� ��������� IHostManager, ������ ����������� IHostManagerInternalOperations");
            
            _fromIp = CurrentOperationContext.RequestIp();

            var messageHeader = MessageHeaderNames.BaseServicePort;
            var port = getValueFromIncomingMessageHeader(messageHeader.ToString());
            if (string.IsNullOrEmpty(port))
            {
                messageHeader = MessageHeaderNames.ClientServicePort;
                port = getValueFromIncomingMessageHeader(messageHeader.ToString());
            }

            if (string.IsNullOrEmpty(port))
            {
                CurrentOperationContext.Channel.Abort();
                throw new Exception("Port is IsNullOrEmpty");
            }

            registerNewSession(port, messageHeader);
        }
        private string getValueFromIncomingMessageHeader(string key)
        {
            if (CurrentOperationContext.IncomingMessageHeaders.FindHeader(key, string.Empty) != -1)
                return CurrentOperationContext.IncomingMessageHeaders.GetHeader<string>(key, string.Empty);
            return string.Empty;
        }

        private void registerNewSession(string port, MessageHeaderNames messageHeader)
        {
            var remoteHostAdress = new HostAdress(_fromIp, port);

            //������ ���������� MessageHeaderNames.BaseServicePort, MessageHeaderNames.ClientServicePort ������� 
            //��� ������������ ������� IncomingOperation.Allow, ���� ��� blockingIncomingOperationException
            //��������� ��� ���� ��� �� ������� ������� �������
            for (int i = 0; i < 50; i++)
            {
                IConnection<TConnectionData, TImplementedContract> contains;
                lock (_hostManagerInternalOperations.ConnectionsSync)
                    contains = _hostManagerInternalOperations.Connections.FirstOrDefault(_ => _.RemoteHostAdress == remoteHostAdress);

                if (contains != null)
                {
                    if (i == 0)
                        L.ExchangerLog.Info("Contains from: {0}, Connections: {1}, IncomingOperation: {2}", remoteHostAdress, _hostManagerInternalOperations.Connections.Count, contains.IncomingOperation);

                    //���������
                    contains.Incoming = CurrentOperationContext;
                    CurrentConnection = contains;

                    if (messageHeader == MessageHeaderNames.BaseServicePort || contains.IncomingOperation == IncomingOperation.Allow)
                        return;

                    Thread.Sleep(30);
                    continue;
                }

                //���� BaseServicePort, ������� ��� �� �������� � Connections � onNewIcomingConnection
                if (messageHeader == MessageHeaderNames.BaseServicePort)
                    break;

                Thread.Sleep(30);
            }

            if (messageHeader == MessageHeaderNames.ClientServicePort)
                blockingIncomingOperationException();

            CurrentConnection = new Connection<TConnectionData, TImplementedContract>
            {
                RemoteHostAdress = remoteHostAdress,
                Incoming = OperationContext.Current
            };

            lock (_hostManagerInternalOperations.ConnectionsSync)
                _hostManagerInternalOperations.Connections.Add(CurrentConnection);

            L.ExchangerLog.Info("Add from: {0}, Connections: {1}", CurrentConnection.RemoteHostAdress, _hostManagerInternalOperations.Connections.Count);

            _hostManagerInternalOperations.OnNewIcomingConnection(CurrentConnection);
        }
        private void blockingIncomingOperationException()
        {
            var error = string.Format("IncomingOperation is NotAllow, FromIp: {0}", _fromIp);
            throw new BlockingIncomingOperationException(error);
        }

        public bool Ping()
        {
            return true;
        }
    }
}