using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.Service.ExchangeDataStore.Core.Settings.SlackNotifications;
// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace Lykke.Service.ExchangeDataStore.Core.Settings
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AppSettings
    {
        public ExchangeDataStoreSettings ExchangeDataStoreService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
