using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class DepositSubscriber
    {
        private readonly ILogger<DepositSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public DepositSubscriber(
            ISubscriber<Deposit> subscriber,
            ILogger<DepositSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            _telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(Deposit deposit)
        {
            using var activity = MyTelemetry.StartActivity("Handle Deposit");

            _logger.LogInformation("Processing Deposit: {@context}", deposit.ToJson());

            try
            {
                var chatId = deposit.Status switch
                {
                    DepositStatus.ManualApprovalPending => Program.Settings.ManualApproveDepositChatId,
                    DepositStatus.Processed => Program.Settings.SuccessDepositChatId,
                    _ => Program.Settings.FailDepositChatId
                };

                if (string.IsNullOrEmpty(chatId))
                    return;

                var status = deposit.Status switch
                {
                    DepositStatus.Error => "Failed ⚠️",
                    DepositStatus.Processed => "Successful",
                    DepositStatus.Cancelled => "Cancelled",
                    DepositStatus.ManualApprovalPending => "MANUAL APPROVAL PENDING",
                    _ => ""
                };

                if (string.IsNullOrEmpty(status)) return;

                var message =
                    $"DEPOSIT {status}! ID:{deposit.Id} {deposit.Amount} {deposit.AssetSymbol} ({deposit.Network}). {Environment.NewLine}" +
                    $" BrokerId: {deposit.BrokerId}; ClientId: {deposit.ClientId}. Retries:  {deposit.RetriesCount}";

                await _telegramBotClient.SendTextMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error FireblocksDepositSignal {@context}", deposit.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}