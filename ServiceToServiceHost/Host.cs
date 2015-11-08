using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ConsoleApplication2
{
    public class Host
    {
        private readonly NetTcpBinding _netTcpBinding = new NetTcpBinding(SecurityMode.Transport)
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferPoolSize = 2147483647,
            Security = new NetTcpSecurity { Mode = SecurityMode.Transport }
        };

        private readonly ServiceHost _serviceHost;
        public ServiceHost ServiceHost {
            get { return _serviceHost; }
        }

        public Host(string hostedAdress)
        {
            _serviceHost = new ServiceHost(typeof(ExchangerService), new Uri(string.Format("net.tcp://{0}/Service", hostedAdress)));
            foreach (var serviceBehavior in _serviceHost.Description.Behaviors.OfType<ServiceMetadataBehavior>())
            {
                serviceBehavior.HttpGetEnabled = false;
                serviceBehavior.HttpsGetEnabled = false;
            }

            _serviceHost.AddServiceEndpoint(typeof(IExchangerService), _netTcpBinding, "");
            _serviceHost.AddServiceEndpoint(typeof(IPing), _netTcpBinding, "/Ping");
            _serviceHost.Description.Behaviors.Add(new NInjectInstanceProvider(new ServicesNinjectModules()));
            _serviceHost.Open();
            Console.WriteLine("Service {0} hosted and {1} to receive requests", _serviceHost.Description.Name, _serviceHost.State);
        }
    }
}