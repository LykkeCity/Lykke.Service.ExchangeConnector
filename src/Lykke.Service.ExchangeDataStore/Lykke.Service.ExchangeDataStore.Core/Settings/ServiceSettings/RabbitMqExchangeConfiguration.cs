// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings
{
    public class RabbitMqExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public string Exchange { get; set; }

        public string Queue { get; set; }

        public string ConnectionString { get; set; }
    }
}
