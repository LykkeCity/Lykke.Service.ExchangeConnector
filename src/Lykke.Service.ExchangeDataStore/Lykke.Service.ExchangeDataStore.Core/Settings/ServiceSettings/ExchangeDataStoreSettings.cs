namespace Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
{
    public class ExchangeDataStoreSettings
    {
        public RabbitMqMultyExchangeConfiguration RabbitMq { get; set; }
        public AzureTableConfiguration AzureStorage { get; set; }
    }
}
