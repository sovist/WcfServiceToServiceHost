namespace ServiceToServiceHost
{
    internal static class ConnectionHalper
    {
        public static string ConnectionAdressToService(string ip, string port, string serviceName)
        {
            return string.Format("net.tcp://{0}:{1}/{2}", ip, port, serviceName);
        }
        public static string ConnectionAdressToBaseService(string ip, string port, string serviceName)
        {
            return string.Format("{0}/BaseService", ConnectionAdressToService(ip, port, serviceName));
        }
    }
}