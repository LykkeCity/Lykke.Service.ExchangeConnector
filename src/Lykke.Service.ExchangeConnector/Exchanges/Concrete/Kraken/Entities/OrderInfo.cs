using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public enum OrderStatus
    {
        Pending,
        Open,
        Closed,
        Canceled,
        Expired
    }
    
    public class OrderInfo
    {
        public OrderStatus Status { get; set; }
        
        [JsonProperty("opentm")]
        public long OpenTime { get; set; }
        
        [JsonProperty("starttm")]
        public long StartTime { get; set; }
        
        [JsonProperty("descr")]
        public OrderDescriptionInfo DescriptionInfo { get; set; }
        
        [JsonProperty("vol")]
        public decimal Volume { get; set; }
        
        [JsonProperty("vol_exec")]
        public decimal VolumeExecuted { get; set; }
        
        public decimal Cost { get; set; }
        
        public decimal Fee { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal StopPrice { get; set; }
        
        public decimal LimitPrice { get; set; }
        
        public string Misc { get; set; }
        
        public string Oflags { get; set; }
        
        public List<string> Trades { get; set; }
    }
}
