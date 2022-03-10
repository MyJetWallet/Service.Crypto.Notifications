using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class WithdrawalSubscriber
    {
        private readonly ILogger<WithdrawalSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        public WithdrawalSubscriber(
            ISubscriber<Withdrawal> subscriber,
            ILogger<WithdrawalSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            this._telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(Withdrawal withdrawal)
        {
            using var activity = MyTelemetry.StartActivity("Handle Withdrawal");

            _logger.LogInformation("Processing Withdrawal: {@context}", withdrawal.ToJson());

            try
            {
                var chatId = withdrawal.Status != WithdrawalStatus.Cancelled ? Program.Settings.SuccessWithdrawalChatId :
                    Program.Settings.FailedWithdrawalChatId;

                if (string.IsNullOrEmpty(chatId))
                    return;

                if (withdrawal.Status == WithdrawalStatus.Cancelled ||
                    withdrawal.Status == WithdrawalStatus.Success ||
                    withdrawal.WorkflowState == WithdrawalWorkflowState.Failed)
                {
                    var error = !string.IsNullOrEmpty(withdrawal.LastError) ? $"Error: {withdrawal.LastError}" : "";
                    await _telegramBotClient.SendTextMessageAsync(chatId,
                    $"WITHDRAWAL {withdrawal.Status}! Transaction Id: {withdrawal.TransactionId} ({withdrawal.AssetSymbol}): {withdrawal.Amount}. BrokerId: {withdrawal.BrokerId}; ClientId: {withdrawal.ClientId}. Retries: {withdrawal.RetriesCount};" +
                    $"WorkflowState: {withdrawal.WorkflowState};" + error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Withdrawal {@context}", withdrawal.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}
