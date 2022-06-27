using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientRiskManager.ServiceBus.FraudDetection;
using Service.Crypto.Notifications.Deduplication;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class FraudDetectedSubscriber
    {
        private readonly ILogger<FraudDetectedSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly LruCache<string, string> _deduplicationCache = new LruCache<string, string>(50, x => x);
        public FraudDetectedSubscriber(
            ISubscriber<FraudDetectedMessage> subscriber,
            ILogger<FraudDetectedSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            this._telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(FraudDetectedMessage message)
        {
            using var activity = MyTelemetry.StartActivity("Handle FraudDetected");

            _logger.LogInformation("Processing FraudDetected: {@context}", message.ToJson());

            try
            {
                if (_deduplicationCache.TryGetItemByKey(message.ClientFraud.ClientId, out _))
                {
                    _logger.LogDebug("FraudDetected deduplicated: {context}", message.ToJson());
                    return;
                }

                var chatId = Program.Settings.CircleChatId;

                if (message.ClientFraud.CardFraudDetected)
                {
                    await _telegramBotClient.SendTextMessageAsync(chatId,
                    $"Card Fraud Detected ⚠️ \r\n" +
                    $"Client is BLOCKED! \r\n" +
                    $"ClientId: {message.ClientFraud.ClientId} \r\n" +
                    $"Type: {message.ClientFraud.Type} \r\n");
                } else
                {
                    await _telegramBotClient.SendTextMessageAsync(chatId,
                    $"Payment Fraud Detected ⚠️ \r\n" +
                    $"Client is BLOCKED! \r\n" +
                    $"ClientId: {message.ClientFraud.ClientId} \r\n" +
                    $"Type: {message.ClientFraud.Type} \r\n" +
                    $"Attempts3dsFailedCount: {message.ClientFraud.Attempts3dsFailedCount} \r\n");
                }

                _deduplicationCache.AddItem(message.ClientFraud.ClientId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error FraudDetected {@context}", message.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}
