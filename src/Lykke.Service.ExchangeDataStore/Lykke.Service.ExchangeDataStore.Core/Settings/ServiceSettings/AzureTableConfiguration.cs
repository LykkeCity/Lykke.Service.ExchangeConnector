namespace Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings
{
    public class AzureTableConfiguration
    {
        public string LogsConnString { get; set; }
        public string LogTableName { get; set; }

        public string EntitiesConnString { get; set; }
        public string EntitiesTableName { get; set; }
        public string EntitiesBlobContainerName { get; set; }



    }
}
