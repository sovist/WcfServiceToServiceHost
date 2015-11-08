using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Security;
using Ninject;

namespace ServiceToServiceHost
{
    internal class NInjectInstanceProvider : IServiceBehavior, IInstanceProvider
    {
        private readonly IKernel _kernel;
        public NInjectInstanceProvider(IKernel kernel)
        {
            _kernel = kernel;
        }
        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            return GetInstance(instanceContext);
        }
        public object GetInstance(InstanceContext instanceContext)
        {
            return _kernel.Get(instanceContext.Host.Description.ServiceType);
        }
        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            _kernel.Release(instance);
        }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {           
        }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {           
        }
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            var channelDispatchers = serviceHostBase.ChannelDispatchers.Cast<ChannelDispatcher>().ToList();
            foreach (var channelDispatcher in channelDispatchers)          
                    channelDispatcher.ErrorHandlers.Add(new ServiceErrorHandler());

            var endpoints = channelDispatchers.SelectMany(cd => cd.Endpoints);
            foreach (var endpoint in endpoints)            
                endpoint.DispatchRuntime.InstanceProvider = this;           
        }
    }

    internal class ServiceErrorHandler : IErrorHandler
    {
        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
        }

        public bool HandleError(Exception error)
        {
            var errType = error.GetType();
            if (errType == typeof (CommunicationObjectAbortedException) ||
                errType == typeof (TimeoutException) ||
                errType == typeof (CommunicationException) ||
                errType == typeof (SecurityNegotiationException))
                return false;

            if (errType == typeof (BlockingIncomingOperationException))
            {
                L.ExchangerLog.Warn("{0}", error.Message);
                return false;
            }

            L.ExchangerLog.Error("{0}", error);                           
            return false;
        }
    }

    internal class BlockingIncomingOperationException : Exception
    {
        public BlockingIncomingOperationException()
        { }
        public BlockingIncomingOperationException(string message) : base(message)
        { }
    }
}