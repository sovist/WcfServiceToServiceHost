using System;
using System.ServiceModel;

namespace ConsoleApplication2
{
    public interface IConnectionToHost<out TService>
    {
        ISafetyServiceCollaboration<TService> ServiceConection { get; }
    }

    public class ConnectionToExchangerHost : IConnectionToHost<IExchangerService>
    {
        private readonly ISafetyServiceCollaboration<IExchangerService> _safetyServiceCollaboration;
        public ISafetyServiceCollaboration<IExchangerService> ServiceConection
        {
            get { return _safetyServiceCollaboration; }
        }

        private readonly EndpointAddress _serviceEndpointAddress;
        private readonly EndpointAddress _pingEndpointAddress;
        private static readonly NetTcpBinding ServiceNetTcpBinding = new NetTcpBinding(SecurityMode.Transport)
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferPoolSize = 2147483647,
            CloseTimeout = TimeSpan.FromSeconds(15),
            OpenTimeout = TimeSpan.FromSeconds(15),
            ReceiveTimeout = TimeSpan.FromMinutes(2),
            SendTimeout = TimeSpan.FromMinutes(2)
        };

        private static readonly NetTcpBinding PingNetTcpBinding = new NetTcpBinding(SecurityMode.None)
        {
            CloseTimeout = TimeSpan.FromSeconds(7),
            OpenTimeout = TimeSpan.FromSeconds(7),
            ReceiveTimeout = TimeSpan.FromMinutes(7),
            SendTimeout = TimeSpan.FromMinutes(7)
        };

        public ConnectionToExchangerHost(string ip, string port)
        {
            _pingEndpointAddress  = new EndpointAddress(ExchangerHost.ConnectionAdressToExchangerPing(ip, port));
            _serviceEndpointAddress = new EndpointAddress(ExchangerHost.ConnectionAdressToExchangerService(ip, port));
            _safetyServiceCollaboration= new SafetyServiceCollaboration<IExchangerService, IPing>(createNewServiceChannelFactory, createNewPingChannelFactory, pingAction);
        }

        private void pingAction(IPing ping)
        {
            ping.Ping();
        }

        private ChannelFactory<IPing> createNewPingChannelFactory()
        {
            return new ChannelFactory<IPing>(PingNetTcpBinding, _pingEndpointAddress);
        }

        private ChannelFactory<IExchangerService> createNewServiceChannelFactory()
        {
            return new ChannelFactory<IExchangerService>(ServiceNetTcpBinding, _serviceEndpointAddress);
        }
    }
}