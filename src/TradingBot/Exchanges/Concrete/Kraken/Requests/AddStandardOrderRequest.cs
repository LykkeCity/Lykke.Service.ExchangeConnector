using System.Collections.Generic;
using System.Globalization;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Trading;
using OrderType = TradingBot.Trading.OrderType;

namespace TradingBot.Exchanges.Concrete.Kraken.Requests
{
    public class AddStandardOrderRequest : IKrakenRequest
    {
        public AddStandardOrderRequest()
        {
            
        }

        public AddStandardOrderRequest(Instrument instrument, TradingSignal tradingSignal)
        {
            Pair = instrument.Name;
            Type = tradingSignal.TradeType;
            OrderType = tradingSignal.OrderType;
            Price = tradingSignal.Price;
            Volume = tradingSignal.Volume;
        }
        
        public string Pair { get; set; }
        
        public TradeType Type { get; set; }
        
        public OrderType OrderType { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal Volume { get; set; }
        
        // leverage

        public IEnumerable<KeyValuePair<string, string>> FormData
        {
            get
            {
                yield return new KeyValuePair<string, string>("pair", Pair);
                
                yield return new KeyValuePair<string, string>("type", Type.ToString().ToLowerInvariant()); // buy/sell
                
                yield return new KeyValuePair<string, string>("ordertype", OrderType.ToString().ToLowerInvariant()); // market/limit
                
                if (OrderType == OrderType.Limit)
                    yield return new KeyValuePair<string, string>("price", Price.ToString("#.0", CultureInfo.InvariantCulture));
                
                yield return new KeyValuePair<string, string>("volume", Volume.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}