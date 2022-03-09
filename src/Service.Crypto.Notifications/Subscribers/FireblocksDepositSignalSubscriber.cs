using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class FireblocksDepositSignalSubscriber
    {
        private readonly ILogger<FireblocksDepositSignalSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public FireblocksDepositSignalSubscriber(
            ISubscriber<FireblocksDepositSignal> subscriber,
            ILogger<FireblocksDepositSignalSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            this._telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(FireblocksDepositSignal deposit)
        {
            using var activity = MyTelemetry.StartActivity("Handle FireblocksDepositSignal");

            _logger.LogInformation("Processing FireblocksDepositSignal: {@context}", deposit.ToJson());

            try
            {
                if (deposit.Status == Fireblocks.Webhook.Domain.Models.Deposits.FireblocksDepositStatus.Completed)
                {
                    await _telegramBotClient.SendTextMessageAsync(Program.Settings.SuccessFireblocksDepositChatId,
                    $"FIREBLOCKS DEPOSIT SUCCESFULL! Transaction Id: {deposit.TransactionId} ({deposit.AssetSymbol} - {deposit.Network}): {deposit.Amount}. BrokerId: {deposit.BrokerId}; ClientId: {deposit.ClientId}");
                }
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
