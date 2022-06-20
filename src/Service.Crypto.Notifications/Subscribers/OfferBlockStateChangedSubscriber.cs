using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.HighYieldEngine.Domain.Models.Messages;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
	public class OfferBlockStateChangedSubscriber
	{
		private static readonly string ChatId = Program.Settings.HighYieldChatId;

		private readonly ILogger<OfferBlockStateChangedSubscriber> _logger;
		private readonly ITelegramBotClient _telegramBotClient;

		public OfferBlockStateChangedSubscriber(
			ISubscriber<OfferBlockStateChanged> subscriber,
			ILogger<OfferBlockStateChangedSubscriber> logger,
			ITelegramBotClient telegramBotClient)
		{
			subscriber.Subscribe(HandleSignal);
			_logger = logger;
			_telegramBotClient = telegramBotClient;
		}

		private async ValueTask HandleSignal(OfferBlockStateChanged message)
		{
			using Activity activity = MyTelemetry.StartActivity("Handle OfferBlockStateChanged");

			_logger.LogInformation("Processing OfferBlockStateChanged: {@context}", message.ToJson());

			try
			{
				await _telegramBotClient.SendTextMessageAsync(ChatId,
					$"Offer \"{message.OfferName}\" ({message.OfferId}) topUp {(message.Blocked ? "BLOCKED" : "UNBLOCKED")}! Amount: {message.Amount}, Max Simple Amount {message.MaxAmount}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handle OfferBlockStateChanged {@context}", message.ToJson());
				ex.FailActivity();
				throw;
			}
		}
	}
}