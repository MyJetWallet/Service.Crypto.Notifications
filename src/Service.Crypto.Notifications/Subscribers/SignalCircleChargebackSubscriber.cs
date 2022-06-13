using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Circle.Webhooks.Domain.Models;
using Service.Crypto.Notifications;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.ClientRiskManager.Subscribers
{

    public class SignalCircleChargebackSubscriber
    {
        private readonly ILogger<SignalCircleChargebackSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public SignalCircleChargebackSubscriber(
            ILogger<SignalCircleChargebackSubscriber> logger,
            ISubscriber<SignalCircleChargeback> subscriber,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            _telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(SignalCircleChargeback signal)
        {
            using var activity = MyTelemetry.StartActivity($"Handle {nameof(SignalCircleChargeback)}");

            _logger.LogInformation("Processing SignalCircleChargeback: {context}", signal.ToJson());

            try
            {
                await _telegramBotClient.SendTextMessageAsync(Program.Settings.CircleChatId,
                   $"CIRCLE CHARGEBACK DETECTED! ClientId: {signal.ClientId} (ChargebackId: {signal.Chargeback.Id}; PaymentId: {signal.Chargeback.PaymentId}; )");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SignalCircleChargeback {@context}", signal.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}

