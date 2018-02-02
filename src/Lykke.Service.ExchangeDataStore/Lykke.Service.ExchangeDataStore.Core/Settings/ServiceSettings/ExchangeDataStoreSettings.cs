namespace Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings
{
    public class ExchangeDataStoreSettings
    {
        public RabbitMqMultyExchangeConfiguration RabbitMq { get; set; }
        public AzureTableConfiguration AzureStorage { get; set; }
    }
}
