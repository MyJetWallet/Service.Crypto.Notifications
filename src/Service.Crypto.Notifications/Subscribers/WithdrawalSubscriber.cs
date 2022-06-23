using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Crypto.Notifications.Deduplication;
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
        private readonly LruCache<long, long> _deduplicationCache = new LruCache<long, long>(1000, x => x);
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
                if (_deduplicationCache.TryGetItemByKey(withdrawal.Id, out _))
                {
                    _logger.LogDebug("Withdrawal deduplicated: {context}", withdrawal.ToJson());
                    return;
                }

                var chatId = withdrawal.Status != WithdrawalStatus.Cancelled ? Program.Settings.SuccessWithdrawalChatId :
                    Program.Settings.FailedWithdrawalChatId;

                if (string.IsNullOrEmpty(chatId))
                    return;

                if (withdrawal.Status == WithdrawalStatus.Cancelled ||
                    withdrawal.Status == WithdrawalStatus.Success ||
                    withdrawal.WorkflowState == WithdrawalWorkflowState.Failed)
                {
                    string mark = "👌";
                    if (withdrawal.Status == WithdrawalStatus.Cancelled || withdrawal.WorkflowState == WithdrawalWorkflowState.Failed)
                    {
                        mark = "⚠️";
                    }

                    var error = !string.IsNullOrEmpty(withdrawal.LastError) ? $"Error: {withdrawal.LastError}" : "";
                    await _telegramBotClient.SendTextMessageAsync(chatId,
                    $"WITHDRAWAL {withdrawal.Status} {mark}! ID: ({withdrawal.Id}) {withdrawal.Amount} ({withdrawal.AssetSymbol})." +
                    $"{Environment.NewLine}BrokerId: {withdrawal.BrokerId}; ClientId: {withdrawal.ClientId}. Retries: {withdrawal.RetriesCount}; {Environment.NewLine}" +
                    $"WorkflowState: {withdrawal.WorkflowState}; {Environment.NewLine}" + error);

                    _deduplicationCache.AddItem(withdrawal.Id);
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
