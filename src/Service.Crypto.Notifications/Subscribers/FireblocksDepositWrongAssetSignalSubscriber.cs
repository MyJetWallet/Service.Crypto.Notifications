using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Crypto.Notifications.Deduplication;
using Service.Fireblocks.Webhook.ServiceBus.Deposits;
using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class FireblocksDepositWrongAssetSignalSubscriber
    {
        private readonly ILogger<FireblocksDepositWrongAssetSignalSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly LruCache<string, string> _deduplicationCache = new LruCache<string, string>(50, x => x);

        public FireblocksDepositWrongAssetSignalSubscriber(
            ISubscriber<FireblocksDepositWrongAssetSignal> subscriber,
            ILogger<FireblocksDepositWrongAssetSignalSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            _telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(FireblocksDepositWrongAssetSignal deposit)
        {
            using var activity = MyTelemetry.StartActivity("Handle FireblocksDepositWrongAssetSignal");

            _logger.LogInformation("Processing FireblocksDepositWrongAssetSignal: {@context}", deposit.ToJson());

            try
            {
                if (deposit.Status == Fireblocks.Webhook.Domain.Models.Deposits.FireblocksDepositStatus.Completed)
                {
                    if (_deduplicationCache.TryGetItemByKey(deposit.TransactionId, out _))
                    {
                        _logger.LogDebug("Withdrawal deduplicated: {context}", deposit.ToJson());
                        return;
                    }

                    await _telegramBotClient.SendTextMessageAsync(Program.Settings.SuccessDepositChatId,
                    $"⚠️ FIREBLOCKS DEPOSIT WRONG ASSET ⚠️ \r\n" +
                    $"Transaction Id: {deposit.TransactionId} ({deposit.AssetSymbol} - {deposit.Network}): {deposit.Amount}. \r\n" +
                    $"BrokerId: {deposit.BrokerId}; ClientId: {deposit.ClientId} \r\n" +
                    $"{deposit.Comment}");

                    _deduplicationCache.AddItem(deposit.TransactionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error FireblocksDepositWrongAssetSignal {@context}", deposit.ToJson());
                ex.FailActivity();
                throw;
            }
        }
    }
}
