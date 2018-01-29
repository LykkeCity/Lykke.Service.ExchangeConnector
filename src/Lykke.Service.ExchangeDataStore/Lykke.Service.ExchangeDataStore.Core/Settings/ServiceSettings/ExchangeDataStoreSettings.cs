namespace Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings
{
    public class ExchangeDataStoreSettings
    {
        public DbSettings Db { get; set; }
        public RabbitMqMultyExchangeConfiguration RabbitMq { get; set; }
    }
}
