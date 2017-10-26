using Microsoft.WindowsAzure.Storage.Table;

namespace TradingBot.Repositories
{
    public class JavaLogEntity : TableEntity
    {
        public string Message { get; set; }

        public string Level { get; set; }
    }
}
