using System.Collections.Generic;
using TradingBot.Common.Trading;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.HistoricalData;
using TradingBot.Exchanges.Concrete.ICMarkets;
using TradingBot.Exchanges.Concrete.Kraken;
using TradingBot.Exchanges.Concrete.StubImplementation;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges
{
    public static class ExchangeFactory
    {
	    public static List<Exchange> CreateExchanges(Configuration config)
	    {
		    var exchanges = CreateExchanges(config.Exchanges);

		    if (config.AzureStorage.Enabled)
			    exchanges.ForEach(x => x.AddTickPriceHandler(new AzureTablePricesPublisher(x.Name, config.AzureStorage.StorageConnectionString)));

		    if (config.RabbitMq.Enabled)
		    {
			    var pricesHandler =
				    new RabbitMqHandler<InstrumentTickPrices>(config.RabbitMq.GetConnectionString(), config.RabbitMq.RatesExchange);

			    var tradesHandler =
				    new RabbitMqHandler<ExecutedTrade>(config.RabbitMq.GetConnectionString(), config.RabbitMq.TradesExchange);
			    
			    exchanges.ForEach(x =>
			    {
				    x.AddTickPriceHandler(pricesHandler);
				    x.AddExecutedTradeHandler(tradesHandler);
			    });
		    }

		    return exchanges;
	    }
	    
        private static List<Exchange> CreateExchanges(ExchangesConfiguration config)
        {
	        var result = new List<Exchange>();
	        
	        if (config.Icm.Enabled)
		        result.Add(new ICMarketsExchange(config.Icm));
	        
	        if (config.Kraken.Enabled)
		        result.Add(new KrakenExchange(config.Kraken));
	        
	        if (config.Stub.Enabled)
		        result.Add(new StubExchange(config.Stub));
	        
	        if (config.HistoricalData.Enabled)
		        result.Add(new HistoricalDataExchange(config.HistoricalData));

	        return result;
        }
    }
}
