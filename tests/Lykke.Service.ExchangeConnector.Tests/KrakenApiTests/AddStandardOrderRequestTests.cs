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
        public void AddOneZeroesToIntPrice()
        {
            var request = new AddStandardOrderRequest(
                new TradingSignal(new Instrument("", ""),  "", OrderCommand.Create, TradeType.Buy, 4000, 1, DateTime.UtcNow, OrderType.Limit));
            
            Assert.Equal("4000.0", request.FormData.First(x => x.Key == "price").Value);
        }
        
        
        [Fact]
        public void PriceHasOneDecimals()
        {
            var request = new AddStandardOrderRequest(
                new TradingSignal(new Instrument("", ""),  "", OrderCommand.Create, TradeType.Buy, 4000.111m, 1, DateTime.UtcNow, OrderType.Limit));
            
            Assert.Equal("4000.1", request.FormData.First(x => x.Key == "price").Value);
        }
    }
}
