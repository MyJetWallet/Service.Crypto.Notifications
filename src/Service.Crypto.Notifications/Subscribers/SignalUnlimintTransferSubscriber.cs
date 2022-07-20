using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Unlimint.Webhooks.Domain.Models;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class SignalUnlimintTransferSubscriber
    {
        private readonly ILogger<SignalUnlimintTransferSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public SignalUnlimintTransferSubscriber(
            ILogger<SignalUnlimintTransferSubscriber> logger,
            ISubscriber<SignalUnlimintTransfer> subscriber,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            _telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(SignalUnlimintTransfer signal)
        {
            using var activity = MyTelemetry.StartActivity($"Handle {nameof(SignalUnlimintTransfer)}");

            _logger.LogInformation("Processing SignalUnlimintTransfer: {context}", signal.ToJson());

            if (signal.PaymentInfo == null || signal.PaymentInfo.Status != MyJetWallet.Unlimint.Models.Payments.PaymentStatus.ChargedBack)
                return;

            try
            {
                await _telegramBotClient.SendTextMessageAsync(Program.Settings.CircleChatId,
                   $"UNLIMINT CHARGEBACK DETECTED! ClientId: {signal.ClientId} (PaymentId: {signal.PaymentInfo.Id}; )");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SignalUnlimintTransfer {@context}", signal.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}
