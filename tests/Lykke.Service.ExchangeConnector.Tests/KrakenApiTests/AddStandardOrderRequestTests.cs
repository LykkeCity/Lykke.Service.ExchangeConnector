using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Exchanges.Concrete.Kraken.Requests;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.KrakenApiTests
{
    public class AddStandardOrderRequestTests
    {
        [Fact]
        public void UseExchangeSymbol()
        {
            var request = new AddStandardOrderRequest(
                new TradingSignal(
                    new Instrument("kraken", "BTCUSD"), 
                    Guid.NewGuid().ToString(), OrderCommand.Create, TradeType.Buy, 4000, 1, DateTime.UtcNow, OrderType.Limit),
                    new List<CurrencySymbol>()
                    {
                        new CurrencySymbol()
                        {
                            LykkeSymbol = "BTCUSD",
                            ExchangeSymbol = "XXBTZUSD"
                        }
                    });
            
            Assert.Equal("XXBTZUSD", request.FormData.Single(x => x.Key == "pair").Value);
        }
    }
}
