using System.ServiceModel;
using System.Threading.Tasks;
using Service.Crypto.Notifications.Grpc.Models;

namespace Service.Crypto.Notifications.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}