using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.HighYieldEngine.Domain.Models;
using Service.HighYieldEngine.Domain.Models.Constants;
using Service.HighYieldEngine.Grpc;
using Service.HighYieldEngine.Grpc.Models.Notification;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
	public class ClientOfferActionSubscriber
	{
		private static readonly string ChatId = Program.Settings.HighYieldChatId;

		private readonly ILogger<ClientOfferActionSubscriber> _logger;
		private readonly ITelegramBotClient _telegramBotClient;
		private readonly IHighYieldEngineNotificationService _notificationService;

		public ClientOfferActionSubscriber(
			ISubscriber<ClientOfferAction> subscriber,
			ILogger<ClientOfferActionSubscriber> logger,
			ITelegramBotClient telegramBotClient, IHighYieldEngineNotificationService notificationService)
		{
			subscriber.Subscribe(HandleSignal);
			_logger = logger;
			_telegramBotClient = telegramBotClient;
			_notificationService = notificationService;
		}

		private async ValueTask HandleSignal(ClientOfferAction clientOfferAction)
		{
			if (clientOfferAction.ClientOfferActionType != ClientOfferActionType.Unsubscribe
				|| clientOfferAction.State == ClientOfferActionState.Executed)
				return;

			using Activity activity = MyTelemetry.StartActivity("Handle ClientOfferAction");

			_logger.LogInformation("Processing ClientOfferAction: {@context}", clientOfferAction.ToJson());

			try
			{
				WithdrawalNotificationInfoGrpcResponse info = await _notificationService.GetWithdrawalNotificationInfo(new GetWithdrawalNotificationInfoGrpcRequest
				{
					Asset = clientOfferAction.AssetSymbol
				});

				if (info?.NotEnough != true)
					return;

				await _telegramBotClient.SendTextMessageAsync(ChatId,
					$"WITHDRAWAL: Not enough funds to return to user's wallet {clientOfferAction.ClientWalletId}" +
						$" for asset {clientOfferAction.AssetSymbol}! ActionId: {clientOfferAction.ActionId}." +
						$"{Environment.NewLine}BrokerId: {clientOfferAction.BrokerId}; ClientId: {clientOfferAction.ClientId}. Retries: {clientOfferAction.RetryCount}; {Environment.NewLine}" +
						$"WorkflowState: {clientOfferAction.WorkFlowState}; {Environment.NewLine}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handle ClientOfferAction {@context}", clientOfferAction.ToJson());
				ex.FailActivity();
				throw;
			}
		}
	}
}