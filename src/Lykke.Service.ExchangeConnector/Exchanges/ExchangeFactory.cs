using System.Collections.Generic;
using System.Linq;
using Common.Log;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges
{
    internal sealed class ExchangeFactory
    {
        private readonly AppSettings _config;
        private readonly IReloadingManager<TradingBotSettings> _settingsManager;
        private readonly IReadOnlyCollection<Exchange> _implementations;
        private readonly ILog _log;

        public ExchangeFactory(AppSettings config, IReloadingManager<TradingBotSettings> settingsManager, IReadOnlyCollection<Exchange> implementations, ILog log)
        {
            _config = config;
            _settingsManager = settingsManager;
            _implementations = implementations;
            _log = log;
        }

        public IReadOnlyCollection<Exchange> CreateExchanges()
        {
            if (_config.AzureStorage.Enabled)
            {
                foreach (var exchange in _implementations.Where(x => x.Config.SaveQuotesToAzure))
                {
                    var exchangeStorage = AzureTableStorage<PriceTableEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.EntitiesConnString), exchange.Name, new LogToConsole());
                    exchange.AddTickPriceHandler(new AzureTablePricesPublisher(exchangeStorage, exchange.Name));
                }
            }

            if (_config.RabbitMq.Enabled && _implementations.Any(x => x.Config.PubQuotesToRabbit))
            {
                foreach (var exchange in _implementations.Where(x => x.Config.PubQuotesToRabbit))
                {
                    var pricesHandler = new RabbitMqHandler<TickPrice>(_config.RabbitMq.GetConnectionString(), _config.RabbitMq.TickPrices.Exchange);
                    var tradesHandler = new RabbitMqHandler<ExecutedTrade>(_config.RabbitMq.GetConnectionString(), _config.RabbitMq.Trades.Exchange);
                    var acknowledgementsHandler = new RabbitMqHandler<Acknowledgement>(_config.RabbitMq.GetConnectionString(), _config.RabbitMq.Acknowledgements.Exchange);
                    
                    exchange.AddTickPriceHandler(pricesHandler);
                    exchange.AddExecutedTradeHandler(tradesHandler);
                    exchange.AddAcknowledgementsHandler(acknowledgementsHandler);
                }
            }

            if (_config.OrderBooksRabbitMq.Enabled)
            {
                foreach (var exchange in _implementations)
                {
                    var orderBookHandler = new RabbitMqHandler<OrderBook>(_config.OrderBooksRabbitMq.GetConnectionString(), _config.OrderBooksRabbitMq.Exchange, _config.OrderBooksRabbitMq.Durable, _log);
                    exchange.AddOrderBookHandler(orderBookHandler);
                }
            }

            return _implementations;
        }
    }
}
