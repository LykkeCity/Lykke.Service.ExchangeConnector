using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.ICMarkets.Converters;
using TradingBot.Exchanges.Concrete.ICMarkets.Entities;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.ICMarkets
{
    public class ICMarketsExchange : Exchange
    {
        public ICMarketsExchange(IcmConfig config) : base("ICMarkets")
        {
            this.config = config;
        }

        private readonly IcmConfig config;

        private RabbitMqSubscriber<OrderBook> rabbit;
        
        public override void ClosePricesStream()
        {
            rabbit?.Stop();
        }

        /// <summary>
        /// For ICM we use internal RabbitMQ exchange with pricefeed
        /// </summary>
        public override Task OpenPricesStream(Instrument[] instruments, Action<InstrumentTickPrices> callback)
        {
            var rabbitSettings = new RabbitMqSubscriberSettings()
            {
                ConnectionString = config.RabbitMq.GetConnectionString(),
                ExchangeName = config.RabbitMq.Exchange
            };

            rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings)
                .SetMessageDeserializer(new OrderBookDeserializer())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new GetPricesCycle.RabbitConsole())
                .SetLogger(new LogToConsole())
                .Subscribe(orderBook =>
                {
                    if (instruments.Any(x => x.Name == orderBook.Asset))
                        callback(orderBook.ToInstrumentTickPrices());
                    
                    return Task.FromResult(0);
                })
                .Start();

            return Task.FromResult(0);
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
