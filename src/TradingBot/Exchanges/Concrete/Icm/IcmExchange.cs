using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Logging;
using Polly;
using QuickFix;
using QuickFix.Transport;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Icm.Converters;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.Icm
{
    public class IcmExchange : Exchange
    {
        public new static readonly string Name = "icm";
        
        public IcmExchange(IcmConfig config) : base(Name, config)
        {
            this.config = config;
        }

        private readonly IcmConfig config;

        private RabbitMqSubscriber<OrderBook> rabbit;
        private SocketInitiator initiator;
        private IcmConnector connector;


        /// <summary>
        /// For ICM we use internal RabbitMQ exchange with pricefeed
        /// </summary>
        public override Task OpenPricesStream()
        {
            //StartFixConnection(); // wait for Fix connection before translationg quotes and receiveng signals
            StartRabbitConnection();

            return Task.FromResult(0);
        }
        
        public override void ClosePricesStream()
        {
            rabbit?.Stop();
            initiator?.Stop();
            initiator?.Dispose();
        }

        private void StartRabbitConnection()
        {
            var rabbitSettings = new RabbitMqSubscriberSettings()
            {
                ConnectionString = config.RabbitMq.GetConnectionString(),
                ExchangeName = config.RabbitMq.RatesExchange
            };

            rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new ExchangeConnectorApplication.RabbitConsole())
                .SetLogger(new LogToConsole())
                .Subscribe(async orderBook =>
                {
                    //Logger.LogInformation($"Receive order book for asset: {orderBook.Asset}");
                    
                    if (Instruments.Any(x => x.Name == orderBook.Asset))
                        await CallHandlers(orderBook.ToInstrumentTickPrices());
                    else
                    {
                        //Logger.LogInformation("It's not in the list");
                    }
                })
                .Start()
                ;
        }
        
        private void StartFixConnection()
        {
            var settings = new SessionSettings(config.GetFixConfigAsReader());
            
            connector = new IcmConnector(config);
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new ScreenLogFactory(settings);

            connector.OnTradeExecuted += CallExecutedTradeHandlers;
            
            initiator = new SocketInitiator(connector, storeFactory, settings, logFactory);
            initiator.Start();
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            StartFixConnection();
            
            var retry = Policy
                .HandleResult<bool>(x => !x)
                .WaitAndRetry(5, attempt => TimeSpan.FromSeconds(10));

            
            return Task.FromResult(retry.Execute(() => initiator.IsLoggedOn));
        }
        
        protected override Task<bool> AddOrder(Instrument instrument, TradingSignal signal)
        {
            Logger.LogInformation($"About to place new order for instrument {instrument}: {signal}");
            return Task.FromResult(connector.AddOrder(instrument, signal));
        }

        protected override Task<bool> CancelOrder(Instrument instrument, TradingSignal signal)
        {
            Logger.LogInformation($"Cancelling order {signal}");

            return Task.FromResult(connector.CancelOrder(instrument, signal));
        }

        public Task<ExecutedTrade> GetOrderInfo(Instrument instrument, long orderId)
        {
            return connector.GetOrderInfoAndWaitResponse(instrument, orderId);
        }

//        public Task<object> GetOrdersCountAsync()
//        {
//            connector.SendOrderStatusRequest();
//
//            return connector.WaitOrderStatusRequest();
//        }

        public Task<IEnumerable<ExecutedTrade>> GetAllOrdersInfo()
        {
            return connector.GetAllOrdersInfo();
        }

        public Task<ExecutedTrade> AddOrderAndWait(Instrument instrument, TradingSignal signal, TimeSpan timeout)
        {
            return connector.AddOrderAndWaitResponse(instrument, signal, timeout);
        }
    }
}
