using System;
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
        private readonly IRabbitMqHandlersFactory _handlersFactory;
        private readonly ILog _log;

        public ExchangeFactory(AppSettings config, IReloadingManager<TradingBotSettings> settingsManager, IReadOnlyCollection<Exchange> implementations, IRabbitMqHandlersFactory handlersFactory, ILog log)
        {
            _config = config;
            _settingsManager = settingsManager;
            _implementations = implementations;
            _handlersFactory = handlersFactory;
            _log = log;
        }

        public IReadOnlyCollection<Exchange> CreateExchanges()
        {
            if (_config.AzureStorage.Enabled)
            {
                foreach (var exchange in _implementations.Where(x => x.Config.SaveQuotesToAzure))
                {
                    var exchangeStorage = AzureTableStorage<PriceTableEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), exchange.Name, new LogToConsole());
                    exchange.AddTickPriceHandler(new AzureTablePricesPublisher(exchangeStorage, exchange.Name));
                }
            }

            foreach (var exchange in _implementations)
            {
                if (_config.RabbitMq.TickPrices.Enabled)
                {
                    var pricesHandler = _handlersFactory.Create<TickPrice>(_config.RabbitMq.TickPrices.ConnectionString, _config.RabbitMq.TickPrices.Exchange, true, _log);
                    exchange.AddTickPriceHandler(pricesHandler);
                }
                if (_config.RabbitMq.Trades.Enabled)
                {
                    var tradesHandler = _handlersFactory.Create<OrderStatusUpdate>(_config.RabbitMq.Trades.ConnectionString, _config.RabbitMq.Trades.Exchange, true, _log);
                    exchange.AddExecutedTradeHandler(tradesHandler);
                }

                if (_config.RabbitMq.Acknowledgements.Enabled)
                {
                    var acknowledgementsHandler = _handlersFactory.Create<OrderStatusUpdate>(_config.RabbitMq.Acknowledgements.ConnectionString, _config.RabbitMq.Acknowledgements.Exchange, true, _log);
                    exchange.AddAcknowledgementsHandler(acknowledgementsHandler);
                }

                if (_config.RabbitMq.OrderBooks.Enabled)
                {
                    var orderBookHandler = _handlersFactory.Create<OrderBook>(_config.RabbitMq.OrderBooks.ConnectionString, _config.RabbitMq.OrderBooks.Exchange, true, _log);
                    exchange.AddOrderBookHandler(orderBookHandler);
                }
            }

            return _implementations;
        }
    }
}
