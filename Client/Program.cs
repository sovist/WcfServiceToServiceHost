using System;
using System.ServiceModel;
using System.Threading;
using Ninject;
using Ninject.Modules;
using NLog;
using ServiceToServiceHost;

namespace Client
{
    internal class Program
    {
        private static readonly EventWaitHandle EventWaitHost2OnIcomingConnection = new EventWaitHandle(false, EventResetMode.AutoReset);
        private static void Main(string[] args)
        {
            string s = new string('-', 30);
            L.AppLog.Info("{0} Start {0}", s);

            ServiceToServiceHost.Logger.SetLoggerInstance(new ServiceHostLogger());

            var host1 = HostManagerFactory.Create<ExchangerService, IExchangerService, MyConnectionData>("8241", new StandardKernel(new TestServicesNinjectModules()));
            host1.IcomingConnection += host1OnIcomingConnection;
            host1.LostConnection += host1OnLostConnection;
            host1.Reconnect += host1OnReconnect;

            var host2 = HostManagerFactory.Create<ExchangerService, IExchangerService, MyConnectionData>("8242", new StandardKernel(new TestServicesNinjectModules()));
            host2.IcomingConnection += host2OnIcomingConnection;
            host2.LostConnection += host2OnLostConnection;
            host2.Reconnect += host2OnReconnect;
                     
            host1.CreateNewConnectToRemoteHost(new HostAdress("localhost", "8242"), IncomingOperation.Allow, new MyConnectionData());
            
            WaitHandle.WaitAny(new WaitHandle[] {EventWaitHost2OnIcomingConnection});

            for (int i = 0; i < 10; i++)
            {
                host1.CallRemoteServiceMethod(data => true,
                    connection =>
                    {
                        var str = string.Format("from host1 {0}", DateTime.Now.ToShortTimeString());
                        var status = connection.Outcoming.Connect.Call(_ => _.TestMethod(str));

                        if (status.CallStatus)
                            L.AppLog.Info("host1 service call, Status: {0}, Rezult: {1}", status.CallStatus, status.Result.ToString());
                        else
                            L.AppLog.Info("host1 service call, Status: {0}", status.CallStatus);
                    });
                
                host2.CallRemoteServiceMethod(data => true,
                    connection =>
                    {
                        var str = string.Format("from host2 {0}", DateTime.Now.ToShortTimeString());
                        var status = connection.Outcoming.Connect.Call(_ => _.TestMethod(str));

                        if (status.CallStatus)
                            L.AppLog.Info("host2 service call, Status: {0}, Rezult: {1}", status.CallStatus,
                                status.Result.ToString());
                        else
                            L.AppLog.Info("host2 service call, Status: {0}", status.CallStatus);
                    });
            }

            Console.ReadKey();
        }

        private static void host2OnReconnect(IConnectionData<MyConnectionData> connectionData)
        {
            L.AppLog.Info("To: {0}", connectionData.RemoteHostAdress);
        }

        private static void host2OnLostConnection(IConnectionData<MyConnectionData> connectionData)
        {
            L.AppLog.Info("To: {0}", connectionData.RemoteHostAdress);
        }

        private static void host2OnIcomingConnection(NewIcomingConnectionEventArgs<MyConnectionData> incomingConnectionEventArgs)
        {
            L.AppLog.Info("From: {0}", incomingConnectionEventArgs.ConnectionData.RemoteHostAdress);
            incomingConnectionEventArgs.ConnectionData.Data = new MyConnectionData();
            incomingConnectionEventArgs.CreateConnectionToThisRemoteHost = true;
            incomingConnectionEventArgs.IncomingOperation = IncomingOperation.Allow;
            EventWaitHost2OnIcomingConnection.Set();
        }

        private static void host1OnReconnect(IConnectionData<MyConnectionData> connectionData)
        {
            L.AppLog.Info("To: {0}", connectionData.RemoteHostAdress);
        }

        private static void host1OnLostConnection(IConnectionData<MyConnectionData> connectionData)
        {
            L.AppLog.Info("To: {0}", connectionData.RemoteHostAdress);
        }

        private static void host1OnIcomingConnection(NewIcomingConnectionEventArgs<MyConnectionData> incomingConnectionEventArgs)
        {
            L.AppLog.Info("From: {0}", incomingConnectionEventArgs.ConnectionData.RemoteHostAdress);
            incomingConnectionEventArgs.ConnectionData.Data = new MyConnectionData();
            incomingConnectionEventArgs.CreateConnectionToThisRemoteHost = true;
            incomingConnectionEventArgs.IncomingOperation = IncomingOperation.Allow;
        }
    }

    internal class TestServicesNinjectModules : NinjectModule
    {
        public override void Load()
        {
            Bind<TestNinjectDependency>().ToMethod(_ => new TestNinjectDependency(Guid.NewGuid().ToString())).InSingletonScope();
        }
    }

    public class MyConnectionData
    {
    }

    public class TestNinjectDependency
    {
        public string TestFild = "TestNinjectDependency - ";
        public TestNinjectDependency()
        {           
        }
        public TestNinjectDependency(string s)
        {
            TestFild += s;            
        }
    }

    [ServiceContract]
    public interface IExchangerService
    {
        [OperationContract]
        bool TestMethod(string adress);

        [OperationContract]
        void TestMethod1(string adress);
    }

    public class ExchangerService : BaseService<ExchangerService, IExchangerService, MyConnectionData>, IExchangerService
    {
        public ExchangerService(IHostManager<ExchangerService, IExchangerService, MyConnectionData> hostManager, TestNinjectDependency testNinjectDependency) : base(hostManager)
        {
            L.AppLog.Info(" testNinjectDependency.TestFild = {0}", testNinjectDependency.TestFild);
        }

        public bool TestMethod(string portAdress)
        {
            if (CurrentConnection == null)
                L.AppLog.Info("TestMethod from {0}, {1}", "-", portAdress);
            else
                L.AppLog.Info("TestMethod from {0}, {1}", CurrentConnection.RemoteHostAdress, portAdress);
            return true;
        }

        public void TestMethod1(string adress)
        {
            if (CurrentConnection == null)
                L.AppLog.Info("TestMethod from {0}, {1}", "-", adress);
            else
                L.AppLog.Info("TestMethod from {0}, {1}", CurrentConnection.RemoteHostAdress, adress);
        }
    }

    public class ServiceHostLogger : ServiceToServiceHost.ILogger
    {
        private readonly NLog.Logger _logger;
        public ServiceHostLogger()
        {
            _logger = LogManager.GetLogger("ExchangerServiceLog");
        }
        public void Append(string message)
        {
            _logger.Info(string.Format("ServiceHost: {0}", message));
        }
    }
}