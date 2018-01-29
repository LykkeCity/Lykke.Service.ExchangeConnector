using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.Service.ExchangeDataStore.Core.Settings.SlackNotifications;

namespace Lykke.Service.ExchangeDataStore.Core.Settings
{
    public class AppSettings
    {
        public ExchangeDataStoreSettings ExchangeDataStoreService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
