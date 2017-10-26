using System;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class LimitOrderResponse
    {
        public Guid Result { get; set; }
        
        public ErrorModel Error { get; set; }
    }
}