using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using System;
using System.Threading.Tasks;
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

            _logger.LogInformation("Processing Depositt: {@context}", deposit.ToJson());

            try
            {
                var chatId = deposit.Status == DepositStatus.Processed ? Program.Settings.SuccessDepositChatId :
                    Program.Settings.FailDepositChatId;

                if (string.IsNullOrEmpty(chatId))
                    return;

                var status = deposit.Status switch
                {
                    DepositStatus.Error => "Failed ⚠️",
                    DepositStatus.Processed => "Succesfull",
                    DepositStatus.Cancelled => "Cancelled",
                    _ => "",
                };

                if (string.IsNullOrEmpty(status))
                {
                    return;
                }

                var message = $"DEPOSIT {status}! ID:{deposit.Id} {deposit.Amount} {deposit.AssetSymbol} ({deposit.Network}). {Environment.NewLine}" +
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
