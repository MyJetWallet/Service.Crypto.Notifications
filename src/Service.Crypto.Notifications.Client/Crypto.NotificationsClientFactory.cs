using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Crypto.Notifications.Grpc;

namespace Service.Crypto.Notifications.Client
{
    [UsedImplicitly]
    public class CryptoNotificationsClientFactory: MyGrpcClientFactory
    {
        public CryptoNotificationsClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IHelloService GetHelloService() => CreateGrpcService<IHelloService>();
    }
}
