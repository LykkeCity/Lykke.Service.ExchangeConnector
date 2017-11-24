using System;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class LimitOrderMessage
    {
        public LimitOrder[] Orders { get; set; }

        public class LimitOrder
        {
            public Order Order { get; set; }
            public Trade[] Trades { get; set; }
        }
        
        public class Order
        {
            public decimal? Price { get; set; }
            public decimal RemainingVolume { get; set; }
            public DateTime? LastMatchTime { get; set; }
            public string Id { get; set; }
            public string ExternalId { get; set; }
            public string AssetPairId { get; set; }
            public string ClientId { get; set; }
            public decimal Volume { get; set; }
            public OrderStatus Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime Registered { get; set; }
        }
        
        public class Trade
        {
            public string ClientId { get; set; }
            public string Asset { get; set; }
            public decimal Volume { get; set; }
            public decimal? Price { get; set; }
            public DateTime Timestamp { get; set; }
            public string OppositeOrderId { get; set; }
            public string OppositeOrderExternalId { get; set; }
            public string OppositeAsset { get; set; }
            public string OppositeClientId { get; set; }
            public decimal OppositeVolume { get; set; }
        }
    }

}
