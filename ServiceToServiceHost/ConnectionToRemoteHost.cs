using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceToServiceHost
{
    internal enum MessageHeaderNames
    {
        /// <summary>
        /// базовый сервис
        /// </summary>
        BaseServicePort,

        /// <summary>
        /// клиенский сервис 
        /// </summary>
        ClientServicePort
    }

    /// <summary>
    /// Подключение к удаленному хосту
    /// </summary>
    /// <typeparam name="TImplementedContract"></typeparam>
    public interface IConnectionToRemoteHost<out TImplementedContract> : IDisposable
    {
        /// <summary>
        /// Адрес удаленного хоста
        /// </summary>
        HostAdress Adress { get; }

        /// <summary>
        /// Подключение
        /// </summary>
        IConnectionToService<TImplementedContract> Connect { get; }
    }

    internal class ConnectionToRemoteHost<TImplementedContract> : IConnectionToRemoteHost<TImplementedContract>
    {
        public HostAdress Adress { get; }
        public IConnectionToService<TImplementedContract> Connect { get; }

        private readonly EndpointAddress _clientServiceEndpointAddress;
        private readonly EndpointAddress _baseServiceEndpointAddress;

        public ConnectionToRemoteHost(HostAdress adress, string localHostPort, string endpointServiceName)
        {
            Adress = adress;
            var headerBaseService = AddressHeader.CreateAddressHeader(MessageHeaderNames.BaseServicePort.ToString(), string.Empty, localHostPort);
            var headerService = AddressHeader.CreateAddressHeader(MessageHeaderNames.ClientServicePort.ToString(), string.Empty, localHostPort);

            _baseServiceEndpointAddress = new EndpointAddress(
                new Uri(ConnectionHalper.ConnectionAdressToBaseService(adress.Ip, adress.Port, endpointServiceName)), 
                EndpointIdentity.CreateDnsIdentity("localhost"), 
                headerBaseService);

            _clientServiceEndpointAddress = new EndpointAddress(
                new Uri(ConnectionHalper.ConnectionAdressToService(adress.Ip, adress.Port, endpointServiceName)), 
                EndpointIdentity.CreateDnsIdentity("localhost"), 
                headerService);
                
            var connectionSettings = new ConnectionSettings
            {
                MaxTryCountCallServiceMethodIfLostConnection = 4,
                OperationTimeOutMiliseconds = 15000,
                PingIntervalMilliseconds = 500
            };
            Connect = new ConnectionToService<TImplementedContract, IBaseService>(createNewServiceChannelFactory, createNewPingChannelFactory, pingAction, connectionSettings);
        }

        private void pingAction(IBaseService baseService)
        {
            baseService.Ping();
        }

        private ChannelFactory<IBaseService> createNewPingChannelFactory()
        {
            return new ChannelFactory<IBaseService>(BindingConfigurations.Client.BaseService, _baseServiceEndpointAddress);
        }

        private ChannelFactory<TImplementedContract> createNewServiceChannelFactory()
        {
            return new ChannelFactory<TImplementedContract>(BindingConfigurations.Client.ClientService, _clientServiceEndpointAddress);
        }

        public void Dispose()
        {
            Connect.Dispose();
        }
    }
}