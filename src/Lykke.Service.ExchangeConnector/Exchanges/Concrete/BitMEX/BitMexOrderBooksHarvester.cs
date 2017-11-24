using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexOrderBooksHarvester : OrderBooksWebSocketHarvester<object, string>
    {
        public BitMexOrderBooksHarvester(string exchangeName, 
            BitMexExchangeConfiguration configuration, 
            ILog log, 
            OrderBookSnapshotsRepository orderBookSnapshotsRepository,
            OrderBookEventsRepository orderBookEventsRepository,
            BitmexSocketSubscriber socketSubscriber) :
            base(exchangeName, configuration, new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log), log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            socketSubscriber.Subscribe(BitmexTopic.OrderBookL2, HandleResponseAsync);
        }

        protected override async Task MessageLoopImpl()
        {
            // OrderBookHarvester reading cycle is not used
        }

        protected override void StartReading()
        {
            // Do not start message reading loop
            // Only measure loop is started
        }

        private async Task HandleResponseAsync(TableResponse table)
        {
            var orderBookItems = table.Data.Select(o => o.ToOrderBookItem()).ToList();
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
