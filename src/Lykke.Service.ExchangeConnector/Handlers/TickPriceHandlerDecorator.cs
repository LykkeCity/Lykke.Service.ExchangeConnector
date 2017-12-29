using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    internal class TickPriceHandlerDecorator : IHandler<TickPrice>
    {
        private readonly IHandler<TickPrice> _rabbitMqHandler;
        private readonly Dictionary<string, AzureTablePricesPublisher> _publishers = new Dictionary<string, AzureTablePricesPublisher>();

        public TickPriceHandlerDecorator(AppSettings config, IReloadingManager<TradingBotSettings> settingsManager, IHandler<TickPrice> rabbitMqHandler, ILog log)
        {
            _rabbitMqHandler = rabbitMqHandler;
            if (config.AzureStorage.Enabled)
            {
                foreach (var exchange in config.SaveQuotesToAzure.Where(kv => kv.Value))
                {
                    var exchangeStorage = AzureTableStorage<PriceTableEntity>.Create(
                        settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), exchange.Key, log);
                    _publishers.Add(exchange.Key, new AzureTablePricesPublisher(exchangeStorage, exchange.Key, log));
                }
            }
        }

        public async Task Handle(TickPrice message)
        {
            await _rabbitMqHandler.Handle(message);
            if (_publishers.TryGetValue(message.Instrument.Exchange, out var publisher))
            {
                await publisher.Handle(message);
            }
        }
    }
}
