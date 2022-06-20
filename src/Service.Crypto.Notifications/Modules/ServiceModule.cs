using Autofac;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Circle.Webhooks.Domain.Models;
using Service.ClientRiskManager.Subscribers;
using Service.Crypto.Notifications.Subscribers;
using Service.Fireblocks.Webhook.ServiceBus;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using Service.HighYieldEngine.Client;
using Service.HighYieldEngine.Domain.Models;
using Service.HighYieldEngine.Domain.Models.Messages;
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

	        const string queueName = "service-crypto-notifications";

	        builder.RegisterMyServiceBusSubscriberSingle<FireblocksWithdrawalSignal>(serviceBusClient,
		        Topics.FireblocksWithdrawalSignalTopic,
		        queueName, TopicQueueType.Permanent);

	        builder.RegisterMyServiceBusSubscriberSingle<FireblocksDepositSignal>(serviceBusClient,
		        Topics.FireblocksDepositSignalTopic,
		        queueName, TopicQueueType.Permanent);

	        builder.RegisterMyServiceBusSubscriberSingle<Deposit>(serviceBusClient,
		        Deposit.TopicName,
		        queueName, TopicQueueType.Permanent);

	        builder.RegisterMyServiceBusSubscriberSingle<Withdrawal>(serviceBusClient,
		        Withdrawal.TopicName,
		        queueName, TopicQueueType.Permanent);

	        builder.RegisterMyServiceBusSubscriberSingle<Verification>(serviceBusClient,
		        Verification.TopicName,
		        queueName, TopicQueueType.Permanent);

	        builder.RegisterMyServiceBusSubscriberSingle<ClientOfferAction>(serviceBusClient,
		        ClientOfferAction.TopicName,
		        queueName, TopicQueueType.Permanent);

	        builder.RegisterMyServiceBusSubscriberSingle<OfferBlockStateChanged>(serviceBusClient,
				OfferBlockStateChanged.TopicName,
		        queueName, TopicQueueType.Permanent);

	        builder
		        .RegisterMyServiceBusSubscriberSingle<SignalCircleChargeback>(
			        serviceBusClient,
			        SignalCircleChargeback.ServiceBusTopicName,
			        "service-crypto-notifications",
			        TopicQueueType.Permanent);

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
               .RegisterType<SignalCircleChargebackSubscriber>()
               .AutoActivate()
               .SingleInstance();

            builder
                .RegisterType<KycSubscriber>()
                .AutoActivate()
                .SingleInstance();

	        builder
		        .RegisterType<ClientOfferActionSubscriber>()
		        .AutoActivate()
		        .SingleInstance();

	        builder
		        .RegisterType<OfferBlockStateChangedSubscriber>()
		        .AutoActivate()
		        .SingleInstance();

	        builder.RegisterHighYieldEngineNotificationService(Program.Settings.HighYieldEngineServiceUrl);
        }
    }
}