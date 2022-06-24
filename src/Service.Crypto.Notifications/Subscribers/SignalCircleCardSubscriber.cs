using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Circle.Wallets.Grpc;
using Service.Circle.Webhooks.Domain.Models;
using Service.Crypto.Notifications;
using Service.Crypto.Notifications.Deduplication;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.ClientRiskManager.Subscribers
{

    public class SignalCircleCardSubscriber
    {
        private readonly ILogger<SignalCircleCardSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly ICircleCardsService _circleCardsService;
        private readonly LruCache<string, string> _deduplicationCache = new LruCache<string, string>(100, x => x);

        public SignalCircleCardSubscriber(
            ILogger<SignalCircleCardSubscriber> logger,
            ISubscriber<SignalCircleCard> subscriber,
            ITelegramBotClient telegramBotClient,
            ICircleCardsService circleCardsService)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            _telegramBotClient = telegramBotClient;
            this._circleCardsService = circleCardsService;
        }

        private async ValueTask HandleSignal(SignalCircleCard signal)
        {
            using var activity = MyTelemetry.StartActivity($"Handle {nameof(SignalCircleCard)}");

            _logger.LogInformation("Processing SignalCircleCard: {context}", signal.ToJson());

            try
            {
                if (signal.Status != MyJetWallet.Circle.Models.Cards.CardStatus.Failed)
                    return;

                if (_deduplicationCache.TryGetItemByKey(signal.CircleCardId, out _))
                {
                    _logger.LogDebug("Withdrawal deduplicated: {context}", signal.CircleCardId);
                    return;
                }

                var card = await _circleCardsService.GetCardByCircleId(new()
                {
                    CircleCardId = signal.CircleCardId,
                });

                if (!card.IsSuccess)
                {
                    if (!card.IsRetriable)
                        return;

                    throw new Exception("Can't get info about circle card " + signal.CircleCardId);
                }

                await _telegramBotClient.SendTextMessageAsync(Program.Settings.CircleChatId,
                   $"CIRCLE CARD FAILED!\r\n" +
                   $"ClientId: {card.Data.ClientId}!\r\n" +
                   $"CardId: {card.Data.CircleCardId}!\r\n" +
                   $"Country: {signal.IssuerCountry};\r\n" +
                   $"ErrorCode: {signal.ErrorCode};\r\n" +
                   $"Fingerprint: {signal.Fingerprint};\r\n" +
                   $"RiskEvaluation: {signal.RiskEvaluation?.Decision} - {signal.RiskEvaluation?.Reason};\r\n");

                _deduplicationCache.AddItem(signal.CircleCardId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SignalCircleCard {@context}", signal.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}

