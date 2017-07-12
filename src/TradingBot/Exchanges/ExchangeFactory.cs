using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.HistoricalData;
using TradingBot.Exchanges.Concrete.ICMarkets;
using TradingBot.Exchanges.Concrete.Kraken;
using TradingBot.Exchanges.Concrete.StubImplementation;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges
{
    public static class ExchangeFactory
    {
	    public static Exchange CreateExchange(Configuration config)
	    {
		    var exchange = CreateExchange(config.Exchanges);

		    if (config.AzureTable.Enabled)
			    exchange.AddTickPriceHandler(new TickPricesAzurePublisher(exchange.Instruments, config.AzureTable));

		    if (config.RabbitMq.Enabled)
		    {
			    exchange.AddTickPriceHandler(new TickPricesRabbitPublisher(config.RabbitMq));
			    exchange.AddExecutedTradeHandler(new ExecutedOrdersRabbitPublisher(config.RabbitMq));
		    }

		    return exchange;
	    }
	    
        private static Exchange CreateExchange(ExchangesConfiguration config)
        {
	        if (config.Icm.Enabled)
		        return new ICMarketsExchange(config.Icm);
	        else if (config.Kraken.Enabled)
		        return new KrakenExchange(config.Kraken);
	        else if (config.Stub.Enabled)
		        return new StubExchange(config.Stub);
	        else if (config.HistoricalData.Enabled)
		        return new HistoricalDataExchange(config.HistoricalData);
	        else
		        return null;
        }
    }
}
