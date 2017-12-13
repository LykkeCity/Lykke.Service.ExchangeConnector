using System;
using System.Linq;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class LimitOrderMessage
    {
        public LimitOrder[] Orders { get; set; }

        public class LimitOrder
        {
            public Order Order { get; set; }
            public Trade[] Trades { get; set; }
            
            public override string ToString()
            {
                return $"{Order}, trades: {string.Join(", ", Trades.Select(x => x.ToString()))}";
            }
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

            public override string ToString()
            {
                return $"Id: {Id}, ExternalId: {ExternalId}, Price: {Price}, Volume: {Volume}, Remaining: {RemainingVolume}, Status: {Status}";
            }
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

            public override string ToString()
            {
                return $"Time: {Timestamp}, Price: {Price}, Volume: {Volume}, Asset: {Asset}, ClientId: {ClientId}, OppClientId: {OppositeClientId}";
            }
        }
    }

}
