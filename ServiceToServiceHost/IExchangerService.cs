using System.ServiceModel;

namespace ConsoleApplication2
{
    [ServiceContract]
    public interface IExchangerService
    {
        [OperationContract]
        void Register(string adress);

        [OperationContract]
        string TestMethod1();

        [OperationContract]
        void TestMethod2();
    }

    public interface IExchangerServiceHandler : IExchangerService
    {
        
    }

    public class ExchangerServiceHandler : IExchangerServiceHandler
    {
        public void Register(string adress)
        {
        }

        public string TestMethod1()
        {
            return "sdgd";
        }

        public void TestMethod2()
        {
        }
    }
}