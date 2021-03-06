using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Crypto.Notifications.Deduplication;
using Telegram.Bot;

namespace Service.Crypto.Notifications.Subscribers
{
    public class DepositSubscriber
    {
        private readonly ILogger<DepositSubscriber> _logger;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly LruCache<long, long> _deduplicationCache = new LruCache<long, long>(1000, x => x);

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

            _logger.LogInformation("Processing Deposit: {context}", deposit.ToJson());

            try
            {
                if (_deduplicationCache.TryGetItemByKey(deposit.Id, out _))
                {
                    _logger.LogDebug("Deposit deduplicated: {context}", deposit.ToJson());
                    return;
                }

                var chatId = deposit.Status switch
                {
                    DepositStatus.ManualApprovalPending => Program.Settings.ManualApproveDepositChatId,
                    DepositStatus.Processed => Program.Settings.SuccessDepositChatId,
                    _ => Program.Settings.FailDepositChatId
                };

                if (string.IsNullOrEmpty(chatId))
                    return;

                var prefixStatus = "";
                var symbol = "";
                var status = "";

                switch (deposit.Status)
                {
                    case DepositStatus.ManualApprovalPending:
                        status = "MANUAL APPROVAL PENDING";
                        break;
                    case DepositStatus.Processed:
                        status = "Successful";
                        symbol = "👌";
                        break;
                    case DepositStatus.Error:
                        status = "Failed";
                        symbol = "⚠️";
                        break;
                    case DepositStatus.Cancelled:
                        status = "Cancelled";
                        break;
                    default:
                        break;
                }

                if (deposit.BeneficiaryClientId == "MinDepositCollector")
                {
                    prefixStatus = "MIN ";
                    symbol = "⚠️";
                }

                if (string.IsNullOrEmpty(status)) return;

                var message =
                    $"{prefixStatus}DEPOSIT {status} {symbol}! ID:{deposit.Id} {deposit.Amount} {deposit.AssetSymbol} ({deposit.Network}). {Environment.NewLine}" +
                    $" BrokerId: {deposit.BrokerId}; ClientId: {deposit.ClientId}. Retries:  {deposit.RetriesCount}";

                await _telegramBotClient.SendTextMessageAsync(chatId, message);

                _deduplicationCache.AddItem(deposit.Id);
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