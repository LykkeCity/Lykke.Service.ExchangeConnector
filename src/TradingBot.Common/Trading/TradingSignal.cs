using System;
using Newtonsoft.Json;

namespace TradingBot.Common.Trading
{
    public enum SignalType
    {
        Long,
        Short
    }

    public enum OrderType
    {
        Market,
        Limit
    }

    public enum TradeType
    {
        Buy,
        Sell
    }

    public class TradingSignal
    {
        [JsonConstructor]
        public TradingSignal(TradeType tradeType, decimal price, decimal count, DateTime time, 
            OrderType orderType = OrderType.Market)
        {
            TradeType = tradeType;
            Price = price;
            Count = count;
            Time = time;
            OrderType = orderType;

            Type = tradeType == TradeType.Sell ? SignalType.Short : SignalType.Long;
        }

        public DateTime Time { get; }

        public SignalType Type { get; }
        
        public OrderType OrderType { get; }
        
        public TradeType TradeType { get; }

        public decimal Price { get; }

        public decimal Count { get; }

        public decimal Amount => Price * Count;

        public override string ToString()
        {
            return $"Type: {Type}, Price: {Price}, Count: {Count}";
        }
    }
}
