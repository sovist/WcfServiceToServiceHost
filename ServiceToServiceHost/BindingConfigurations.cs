using System;
using System.ServiceModel;

namespace ServiceToServiceHost
{
    internal static class BindingConfigurations
    {
        private static readonly SecurityMode SecurityMode = SecurityMode.None;
        private static readonly TcpClientCredentialType ClientCredentialType = TcpClientCredentialType.None;
        public static class Host
        {
            public static NetTcpBinding BaseService
            {
                get
                {
                   var binding = new NetTcpBinding(SecurityMode);
                   binding.Security.Transport.ClientCredentialType = ClientCredentialType;
                   return binding;
                }
            }
            public static NetTcpBinding ClientService
            {
                get
                {
                    var binding = new NetTcpBinding(SecurityMode)
                    {
                        MaxReceivedMessageSize = 2147483647,
                        MaxBufferPoolSize = 2147483647
                    };
                    binding.Security.Transport.ClientCredentialType = ClientCredentialType;
                    return binding;
                }
            } 
        }

        public static class Client
        {
            public static NetTcpBinding BaseService
            {
                get
                {
                    var binding = new NetTcpBinding(SecurityMode)
                    {
                        CloseTimeout = TimeSpan.FromSeconds(7),
                        OpenTimeout = TimeSpan.FromSeconds(7),
                        ReceiveTimeout = TimeSpan.FromMinutes(7),
                        SendTimeout = TimeSpan.FromMinutes(7)
                    };
                    binding.Security.Transport.ClientCredentialType = ClientCredentialType;
                    return binding;
                }
            }

            public static NetTcpBinding ClientService
            {
                get
                {
                    var binding = new NetTcpBinding(SecurityMode)
                    {
                        MaxReceivedMessageSize = 2147483647,
                        MaxBufferPoolSize = 2147483647,
                        CloseTimeout = TimeSpan.FromSeconds(15),
                        OpenTimeout = TimeSpan.FromSeconds(15),
                        ReceiveTimeout = TimeSpan.FromMinutes(2),
                        SendTimeout = TimeSpan.FromMinutes(2)
                    };
                    binding.Security.Transport.ClientCredentialType = ClientCredentialType;
                    return binding;
                }
            }
        }
    }
}
