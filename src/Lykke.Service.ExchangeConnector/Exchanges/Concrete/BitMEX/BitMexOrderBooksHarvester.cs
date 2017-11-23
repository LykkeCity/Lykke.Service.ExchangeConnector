using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexOrderBooksHarvester : OrderBooksWebSocketHarvester<object, string>
    {
        private readonly IExchangeConfiguration _configuration;

        public BitMexOrderBooksHarvester(string exchangeName, BitMexExchangeConfiguration configuration, ILog log, OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository) :
            base(exchangeName, configuration, new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log), log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;
        }

        public new async Task HandleOrdebookSnapshotAsync(string pair, DateTime timeStamp, IEnumerable<OrderBookItem> orders)
        {
            await base.HandleOrdebookSnapshotAsync(pair, timeStamp, orders);
        }

        public new async Task HandleOrdersEventsAsync(string pair, OrderBookEventType orderEventType,
            IReadOnlyCollection<OrderBookItem> orders)
        {
            await base.HandleOrdersEventsAsync(pair, orderEventType, orders);
        }

        public async Task LogMeasures()
        {
            await Measure();
        }

        protected override async Task Measure()
        {
            var msgInSec = _receivedMessages / MEASURE_PERIOD_SEC;
            var pubInSec = _publishedToRabbit / MEASURE_PERIOD_SEC;
            await Log.WriteInfoAsync(nameof(OrderBooksHarvesterBase),
                $"Receive rate from {ExchangeName} {msgInSec} per second, publish rate to " +
                $"RabbitMq {pubInSec} per second", string.Empty);
            _receivedMessages = 0;
            _publishedToRabbit = 0;
        }

        protected override async Task MessageLoopImpl()
        {
            // OrderBookHarvester reading cycle is not used
        }
    }
}
