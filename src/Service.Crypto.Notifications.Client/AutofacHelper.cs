using Autofac;
using Service.Crypto.Notifications.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Crypto.Notifications.Client
{
    public static class AutofacHelper
    {
        public static void RegisterCryptoNotificationsClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new CryptoNotificationsClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
