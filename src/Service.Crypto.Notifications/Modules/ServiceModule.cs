using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using MyJetWallet.Sdk.ServiceBus;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Crypto.Notifications.Subscribers;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using Service.KYC.Domain.Models;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var telegramBot = new TelegramBotClient(Program.Settings.BotApiKey);

            builder.RegisterInstance(telegramBot).As<ITelegramBotClient>().SingleInstance();

            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(
                Program.ReloadedSettings(e => e.SpotServiceBusHostPort),
                Program.LogFactory);

            builder.RegisterMyServiceBusSubscriberSingle<FireblocksWithdrawalSignal>(serviceBusClient,
                Service.Fireblocks.Webhook.ServiceBus.Topics.FireblocksWithdrawalSignalTopic,
                "service-crypto-notifications", MyServiceBus.Abstractions.TopicQueueType.Permanent);

            builder.RegisterMyServiceBusSubscriberSingle<FireblocksDepositSignal>(serviceBusClient,
                Service.Fireblocks.Webhook.ServiceBus.Topics.FireblocksDepositSignalTopic,
                "service-crypto-notifications", MyServiceBus.Abstractions.TopicQueueType.Permanent);

            builder.RegisterMyServiceBusSubscriberSingle<Deposit>(serviceBusClient,
                Deposit.TopicName,
                "service-crypto-notifications", MyServiceBus.Abstractions.TopicQueueType.Permanent);

            builder.RegisterMyServiceBusSubscriberSingle<Withdrawal>(serviceBusClient,
                Withdrawal.TopicName,
                "service-crypto-notifications", MyServiceBus.Abstractions.TopicQueueType.Permanent);

            builder.RegisterMyServiceBusSubscriberSingle<Verification>(serviceBusClient,
                Verification.TopicName,
                "service-crypto-notifications", MyServiceBus.Abstractions.TopicQueueType.Permanent);
            
            builder
               .RegisterType<DepositSubscriber>()
               .AutoActivate()
               .SingleInstance();

            builder
               .RegisterType<WithdrawalSubscriber>()
               .AutoActivate()
               .SingleInstance();

            builder
               .RegisterType<FireblocksDepositSignalSubscriber>()
               .AutoActivate()
               .SingleInstance();

            builder
               .RegisterType<FireblocksWithdrawalSignalSubscriber>()
               .AutoActivate()
               .SingleInstance();
            
            builder
                .RegisterType<KycSubscriber>()
                .AutoActivate()
                .SingleInstance();
        }
    }
}