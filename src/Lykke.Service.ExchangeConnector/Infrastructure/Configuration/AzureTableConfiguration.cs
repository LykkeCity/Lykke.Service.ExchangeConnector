namespace TradingBot.Infrastructure.Configuration
{
    public class AzureTableConfiguration
    {
        public bool Enabled { get; set; }

        public string LogsConnString { get; set; }
        
        public string EntitiesConnString { get; set; }

        public string LogTableName { get; set; } = "logsExchangeConnector";

        public string TranslatedSignalsTableName { get; set; }
    }
}
