using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.KYC.Domain.Models;
using Service.KYC.Domain.Models.Enum;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class KycSubscriber
    {
        private readonly ILogger<KycSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public KycSubscriber(
            ISubscriber<Verification> subscriber,
            ILogger<KycSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            _telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(Verification verification)
        {
            using var activity = MyTelemetry.StartActivity("Handle Verification");

            _logger.LogInformation("Processing Verification: {@context}", verification.ToJson());

            try
            {
                var chatId = Program.Settings.KycChatId;

                if (string.IsNullOrEmpty(chatId))
                    return;

                var status = verification.VerificationStatus switch
                {
                    VerificationStatus.PreCheckPending => "PreCheck review pending️",
                    VerificationStatus.ManualConfirmPending => "Manual review pending",
                    _ => ""
                };

                if (string.IsNullOrEmpty(status)) return;

                var message =
                    $"{status} for verification ID:{verification.VerificationId}; ClientId {verification.ClientId}; Verifications types {verification.VerificationTypes}; Started at {verification.StartingTime.ToString("f")} {Environment.NewLine}";
                await _telegramBotClient.SendTextMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Verification {@context}", verification.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}