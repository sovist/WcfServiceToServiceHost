using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceToServiceHost
{
    public static class OperationContextExtension
    {
        public static string RequestIp(this OperationContext operationContext)
        {
            var endpointProperty = operationContext.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            var ip = endpointProperty != null ? endpointProperty.Address : string.Empty;
            if (string.IsNullOrEmpty(ip) || ip == "::1")
                return "localhost";

            return ip;
        }
    }
}