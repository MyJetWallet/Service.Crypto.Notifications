using System.Runtime.Serialization;
using Service.Crypto.Notifications.Domain.Models;

namespace Service.Crypto.Notifications.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}