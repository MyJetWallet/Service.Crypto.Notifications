using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class FireblocksWithdrawalSignalSubscriber
    {
        private readonly ILogger<FireblocksWithdrawalSignalSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public FireblocksWithdrawalSignalSubscriber(
            ISubscriber<FireblocksWithdrawalSignal> subscriber,
            ILogger<FireblocksWithdrawalSignalSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            this._telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(FireblocksWithdrawalSignal withdrawal)
        {
            using var activity = MyTelemetry.StartActivity("Handle FireblocksWithdrawalSignal");

            _logger.LogInformation("Processing FireblocksWithdrawalSignal: {@context}", withdrawal.ToJson());

            try
            {
                if (withdrawal.Status == Fireblocks.Webhook.Domain.Models.Withdrawals.FireblocksWithdrawalStatus.Completed)
                {
                    await _telegramBotClient.SendTextMessageAsync(Program.Settings.SuccessFireblocksWithdrawalChatId,
                    $"FIREBLOCKS WITHDRAWAL SUCCESFULL! Transaction Id: {withdrawal.TransactionId} ({withdrawal.AssetSymbol} - {withdrawal.Network}): {withdrawal.Amount}. Id in our system: {withdrawal.ExternalId}");
                }
                else
                {
                    await _telegramBotClient.SendTextMessageAsync(Program.Settings.FailFireblocksWithdrawalChatId,
                    $"FIREBLOCKS WITHDRAWAL FAILED! Transaction Id: {withdrawal.TransactionId} ({withdrawal.AssetSymbol} - {withdrawal.Network}): {withdrawal.Amount}. Id in our system: {withdrawal.ExternalId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error FireblocksWithdrawalSignal {@context}", withdrawal.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}
