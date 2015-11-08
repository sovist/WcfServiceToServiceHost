using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ConsoleApplication2
{

    public interface IHost
    {
        ServiceHost ServiceHost { get; }
        bool Run();
    }

    public class ExchangerHost : IHost
    {
        private static readonly NetTcpBinding ServiceNetTcpBinding = new NetTcpBinding(SecurityMode.Transport)
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferPoolSize = 2147483647
        };

        private static readonly NetTcpBinding PingNetTcpBinding = new NetTcpBinding(SecurityMode.None);

        private readonly ServiceHost _serviceHost;
        public ServiceHost ServiceHost { get { return _serviceHost; } }

        public ExchangerHost(IServiceBehavior serviceBehavior, string hostedPort)
        {
            _serviceHost = new ServiceHost(typeof(ExchangerService));
            foreach (var serviceBeh in _serviceHost.Description.Behaviors.OfType<ServiceMetadataBehavior>())
            {
                serviceBeh.HttpGetEnabled = false;
                serviceBeh.HttpsGetEnabled = false;
            }
            _serviceHost.AddServiceEndpoint(typeof(IExchangerService), ServiceNetTcpBinding, new Uri(ConnectionAdressToExchangerService("localhost", hostedPort)));
            _serviceHost.AddServiceEndpoint(typeof(IPing), PingNetTcpBinding, new Uri(ConnectionAdressToExchangerPing("localhost", hostedPort)));
            _serviceHost.Description.Behaviors.Add(serviceBehavior);
        }

        public bool Run()
        {
            try
            {
                _serviceHost.Open();
                L.ExchangerLog.Info("ExchangerServiceHost is Running, Listening port: {0}", _serviceHost.Description.Endpoints[0].Address.Uri.Port);
                return true;
            }
            catch (Exception ex)
            {
                L.ExchangerLog.Info("ExchangerServiceHost running error. Listening port: {0}, Ex: {1}, {2}", _serviceHost.Description.Endpoints[0].Address.Uri.Port, ex.GetType(), ex.Message);
            }
            return false;
        }

        public static string ConnectionAdressToExchangerService(string ip, string port)
        {
            return string.Format("net.tcp://{0}:{1}/Exchanger", ip, port);
        }
        public static string ConnectionAdressToExchangerPing(string ip, string port)
        {
            return string.Format("net.tcp://{0}:{1}/Exchanger/Ping", ip, port);
        }
    }
}