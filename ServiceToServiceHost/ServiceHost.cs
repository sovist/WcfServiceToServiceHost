using System;
using System.Linq;
using System.ServiceModel.Description;

namespace ServiceToServiceHost
{
    public interface IHost
    {
        System.ServiceModel.ServiceHost Instance { get; }
        bool Run();
    }

    internal class ServiceHost<TService, TImplementedContract, TConnectionData> : IHost 
        where TService : BaseService<TService, TImplementedContract, TConnectionData>, TImplementedContract
    {
        public System.ServiceModel.ServiceHost Instance { get; private set; }

        public ServiceHost(IServiceBehavior serviceBehavior, string hostedPort, string endpointServiceName)
        {
            Instance = new System.ServiceModel.ServiceHost(typeof(TService));
            //Instance.Credentials.ServiceCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindByIssuerName, "Contoso.com");

            foreach (var serviceBeh in Instance.Description.Behaviors.OfType<ServiceMetadataBehavior>())
            {
                serviceBeh.HttpGetEnabled = false;
                serviceBeh.HttpsGetEnabled = false;
            }

            Instance.AddServiceEndpoint(
                typeof(TImplementedContract), 
                BindingConfigurations.Host.ClientService, 
                new Uri(ConnectionHalper.ConnectionAdressToService("localhost", hostedPort, endpointServiceName)));

            Instance.AddServiceEndpoint(
                typeof(IBaseService), 
                BindingConfigurations.Host.BaseService, 
                new Uri(ConnectionHalper.ConnectionAdressToBaseService("localhost", hostedPort, endpointServiceName)));

            Instance.Description.Behaviors.Add(serviceBehavior);
        }

        public bool Run()
        {
            var hostTypeName = typeof (TService).UnderlyingSystemType.Name;
            try
            {
                Instance.Open();
                L.Log.Info("{0} Host is Running, Listening port: {1}", hostTypeName, Instance.Description.Endpoints[0].Address.Uri.Port);
                return true;
            }
            catch (Exception ex)
            {
                L.Log.Info("{0} Host running error. Listening port: {1}, Ex: {2}, {3}", hostTypeName, Instance.Description.Endpoints[0].Address.Uri.Port, ex.GetType(), ex.Message);
            }
            return false;
        }
    }
}