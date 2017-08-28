using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Kraken.Responses
{
    public class AddStandardOrderResponse
    {
        [JsonProperty("descr")]
        public OrderDescriptionInfo DescriptionInfo { get; set; }
        
        public string[] TxId { get; set; }
    }

    public class OrderDescriptionInfo
    {
        public string Order { get; set; }
        
        public string Close { get; set; }
    }
}