using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges
{
    public static class ExchangeFactory
    {
        public static Exchange CreateExchange(ExchangeConfiguration config)
        {
			Exchange exchange;

            switch (config.Name)
			{
				case "kraken":
					exchange = new Concrete.Kraken.KrakenExchange();
					break;
				default:
                    exchange = new Concrete.StubImplementation.StubExchange();
					break;
			}

            return exchange;
        }
    }
}
