﻿using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
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

        public FireblocksDepositWrongAssetSignalSubscriber(
            ISubscriber<FireblocksDepositWrongAssetSignal> subscriber,
            ILogger<FireblocksDepositWrongAssetSignalSubscriber> logger,
            ITelegramBotClient telegramBotClient)
        {
            subscriber.Subscribe(HandleSignal);
            _logger = logger;
            this._telegramBotClient = telegramBotClient;
        }

        private async ValueTask HandleSignal(FireblocksDepositWrongAssetSignal deposit)
        {
            using var activity = MyTelemetry.StartActivity("Handle FireblocksDepositWrongAssetSignal");

            _logger.LogInformation("Processing FireblocksDepositWrongAssetSignal: {@context}", deposit.ToJson());

            try
            {
                if (deposit.Status == Fireblocks.Webhook.Domain.Models.Deposits.FireblocksDepositStatus.Completed)
                {
                    await _telegramBotClient.SendTextMessageAsync(Program.Settings.SuccessDepositChatId,
                    $"⚠️ FIREBLOCKS DEPOSIT WRONG ASSET ⚠️ \r\n" +
                    $"Transaction Id: {deposit.TransactionId} ({deposit.AssetSymbol} - {deposit.Network}): {deposit.Amount}. \r\n" +
                    $"BrokerId: {deposit.BrokerId}; ClientId: {deposit.ClientId} \r\n" +
                    $"{deposit.Comment}");
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
