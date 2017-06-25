using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.ICMarkets;
using TradingBot.Exchanges.Concrete.Kraken;
using TradingBot.Exchanges.Concrete.StubImplementation;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges
{
    public static class ExchangeFactory
    {
        public static Exchange CreateExchange(ExchangesConfiguration config)
        {
	        if (config.Icm.Enabled)
		        return new ICMarketsExchange(config.Icm);
	        else if (config.Kraken.Enabled)
		        return new KrakenExchange(config.Kraken);
	        else if (config.Stub.Enabled)
		        return new StubExchange(config.Stub);
	        else
		        return null;
        }
    }
}
