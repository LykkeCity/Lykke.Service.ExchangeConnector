namespace TradingBot.Common.Configuration
{
    public class AzureTableConfiguration
    {
        public bool Enabled { get; set; }

        public string StorageConnectionString { get; set; }

        public string TableName { get; set; }

        public string AssetsTableName { get; } = "Assets";
    }
}
