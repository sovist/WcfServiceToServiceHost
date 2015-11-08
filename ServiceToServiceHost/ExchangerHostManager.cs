using System.ServiceModel.Description;
using System.Threading;
using Ninject;

namespace ConsoleApplication2
{
    public interface IHostManager
    {
        Connections<OutcomingConnection> OutcomingConnections { get; }
        Connections<IncomingConnection> IncomingConnections { get; }
         IHost Host { get; }
    }

    class ExchangerHostManager : IHostManager
    {
        public Connections<OutcomingConnection> OutcomingConnections { get; private set; }
        public Connections<IncomingConnection> IncomingConnections { get; private set; }
        public IHost Host { get { return _exchangerHost; }}

        private IHost _exchangerHost;
        private readonly string _hostingPort;
        private readonly Thread _hostThread;
        private readonly IServiceBehavior _serviceBehavior;
        private bool _isRunning;
        public string HostingPort { get { return _hostingPort; } }
        public ExchangerHostManager(string hostingPort, IExchangerServiceHandler exchangerServiceHandler)
        {
            OutcomingConnections = new ConnectionsToExchangerHost();
            IncomingConnections = new ExchangerHostConnections();

            _serviceBehavior = new NInjectInstanceProvider(new StandardKernel(new ServicesNinjectModules(exchangerServiceHandler, OutcomingConnections, IncomingConnections)));
            _hostingPort = hostingPort;
            _hostThread = new Thread(host);
            _hostThread.Start();

            while (!_isRunning) //блокируем поток, ждем запуска                 
                Thread.Sleep(50);          
        }

        private void host()
        {
            _exchangerHost = new ExchangerHost(_serviceBehavior, _hostingPort);
            _exchangerHost.Run();
            _isRunning = true;
        }
    }
}
