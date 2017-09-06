using System;
using System.Linq;
using TradingBot.Exchanges.Concrete.Kraken.Requests;
using TradingBot.Trading;
using Xunit;

namespace TradingBot.Tests.KrakenApiTests
{
    public class AddStandardOrderRequestTests
    {
        [Fact]
        public void AddTwoZeroesToIntPrice()
        {
            var request = new AddStandardOrderRequest(new Instrument("", ""),
                new TradingSignal("", OrderCommand.Create, TradeType.Buy, 4000, 1, DateTime.UtcNow, OrderType.Limit));
            
            Assert.Equal("4000.00", request.FormData.First(x => x.Key == "price").Value);
        }
        
        
        [Fact]
        public void PriceHasTwoDecimals()
        {
            var request = new AddStandardOrderRequest(new Instrument("", ""),
                new TradingSignal("", OrderCommand.Create, TradeType.Buy, 4000.111m, 1, DateTime.UtcNow, OrderType.Limit));
            
            Assert.Equal("4000.11", request.FormData.First(x => x.Key == "price").Value);
        }
    }
}