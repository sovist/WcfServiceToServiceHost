using System.ServiceModel;

namespace ConsoleApplication2
{
    [ServiceContract]
    public interface IPing
    {

        [OperationContract]
        bool Ping();
    }
}
