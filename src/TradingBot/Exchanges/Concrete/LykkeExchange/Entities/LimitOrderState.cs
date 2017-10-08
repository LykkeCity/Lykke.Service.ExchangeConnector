using System;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class LimitOrderState
    {
        public Guid Id { get; set; }
        
        public Guid ClientId { get; set; }
        
        public LimitOrderStatus Status { get; set; }
        
        public string AssetPairId { get; set; }
        
        public decimal Volume { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal RemainingVolume { get; set; }
        
        public DateTime? LastMatchTime { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        
        public DateTime? Registered { get; set; }
    }

    public enum LimitOrderStatus
    {
        Pending,
        InOrderBook,
        Processing,
        Matched,
        NotEnoughFunds,
        NoLiquidity,
        UnknownAsset,
        Cancelled
    }
}