using MyJetWallet.Sdk.Service;
using MyYamlParser;
using Telegram.Bot.Types;

namespace Service.Crypto.Notifications.Settings
{
    public class SettingsModel
    {
        [YamlProperty("CryptoNotifications.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("CryptoNotifications.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("CryptoNotifications.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("CryptoNotifications.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("CryptoNotifications.SuccessFireblocksWithdrawalChatId")]
        public string SuccessFireblocksWithdrawalChatId { get; set; }

        [YamlProperty("CryptoNotifications.FailFireblocksWithdrawalChatId")]
        public string FailFireblocksWithdrawalChatId { get; set; }

        [YamlProperty("CryptoNotifications.SuccessWithdrawalChatId")]
        public string SuccessWithdrawalChatId { get; set; }

        [YamlProperty("CryptoNotifications.FailedWithdrawalChatId")]
        public string FailedWithdrawalChatId { get; set; }

        [YamlProperty("CryptoNotifications.BotApiKey")]
        public string BotApiKey { get; set; }

        [YamlProperty("CryptoNotifications.SuccessFireblocksDepositChatId")]
        public string SuccessFireblocksDepositChatId { get; set; }

        [YamlProperty("CryptoNotifications.FailFireblocksDepositChatId")]
        public string FailFireblocksDepositChatId { get; set; }

        [YamlProperty("CryptoNotifications.ManualApproveDepositChatId")]
        public string ManualApproveDepositChatId { get; set; }
        
        [YamlProperty("CryptoNotifications.SuccessDepositChatId")]
        public string SuccessDepositChatId { get; set; }

        [YamlProperty("CryptoNotifications.FailDepositChatId")]
        public string FailDepositChatId { get; set; }
        
        [YamlProperty("CryptoNotifications.KycChatId")]
        public string KycChatId { get; set; }
    }
}
