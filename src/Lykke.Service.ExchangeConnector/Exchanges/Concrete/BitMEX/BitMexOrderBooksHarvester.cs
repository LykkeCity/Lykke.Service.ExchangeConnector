using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexOrderBooksHarvester : OrderBooksWebSocketHarvester<object, string>
    {
        private readonly IBitmexSocketSubscriber _socketSubscriber;

        public BitMexOrderBooksHarvester(
            BitMexExchangeConfiguration configuration,
            ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository,
            OrderBookEventsRepository orderBookEventsRepository,
            IBitmexSocketSubscriber socketSubscriber,
            IHandler<OrderBook> orderBookHandler) :
            base(BitMexExchange.Name, configuration, 
                new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log), log, orderBookSnapshotsRepository, orderBookEventsRepository, orderBookHandler)
        {
            _socketSubscriber = socketSubscriber;

        }


        protected override Task MessageLoopImpl()
        {
            // OrderBookHarvester reading cycle is not used
            return Task.CompletedTask;
        }

        protected override void StartReading()
        {
            // Do not start message reading loop
            // Only measure loop is started
        }

        public override void Start()
        {
            _socketSubscriber.Subscribe(BitmexTopic.orderBookL2, HandleResponseAsync);
            _socketSubscriber.Start();
            base.Start();
        }

        public override void Stop()
        {
            _socketSubscriber.Stop();
            base.Stop();
        }

        private async Task HandleResponseAsync(TableResponse table)
        {
            var orderBookItems = table.Data.Select(BitMexModelConverter.ConvertBookItem).ToList();
            var groupByPair = orderBookItems.GroupBy(ob => ob.Symbol);

            switch (table.Action)
            {
                case Action.Partial:
                    foreach (var symbolGroup in groupByPair)
                    {
                        await HandleOrdebookSnapshotAsync(symbolGroup.Key, DateTime.UtcNow, orderBookItems);
                    }
                    break;
                case Action.Update:
                case Action.Insert:
                case Action.Delete:
                    foreach (var symbolGroup in groupByPair)
                    {
                        await HandleOrdersEventsAsync(symbolGroup.Key, ActionToOrderBookEventType(table.Action), orderBookItems);
                    }
                    break;
                default:
                    await Log.WriteWarningAsync(nameof(HandleResponseAsync), "Parsing order book table response", $"Unknown table action {table.Action}");
                    break;
            }
        }

        private OrderBookEventType ActionToOrderBookEventType(Action action)
        {
            switch (action)
            {
                case Action.Update:
                    return OrderBookEventType.Update;
                case Action.Insert:
                    return OrderBookEventType.Add;
                case Action.Delete:
                    return OrderBookEventType.Delete;
                case Action.Unknown:
                case Action.Partial:
                    throw new NotSupportedException($"Order action {action} cannot be converted to OrderBookEventType");
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}
