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

        public ExchangeFactory(AppSettings config, IReloadingManager<TradingBotSettings> settingsManager, IReadOnlyCollection<Exchange> implementations)
        {
            _config = config;
            _settingsManager = settingsManager;
            _implementations = implementations;
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

            if (_config.RabbitMq.Enabled)
            {
                var pricesHandler = new RabbitMqHandler<InstrumentTickPrices>(_config.RabbitMq.GetConnectionString(), _config.RabbitMq.RatesExchange);
                var tradesHandler = new RabbitMqHandler<ExecutedTrade>(_config.RabbitMq.GetConnectionString(), _config.RabbitMq.TradesExchange);
                var acknowledgementsHandler = new RabbitMqHandler<Acknowledgement>(_config.RabbitMq.GetConnectionString(), _config.RabbitMq.AcknowledgementsExchange);

                foreach (var exchange in _implementations.Where(x => x.Config.PubQuotesToRabbit))
                {
                    exchange.AddTickPriceHandler(pricesHandler);
                    exchange.AddExecutedTradeHandler(tradesHandler);
                    exchange.AddAcknowledgementsHandler(acknowledgementsHandler);
                }
            }

            return _implementations;
        }

    }
}
