using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json.Linq;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Repositories;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexOrderBooksHarvester : OrderBooksHarvesterBase
    {
        private readonly IExchangeConfiguration _configuration;

        public BitMexOrderBooksHarvester(BitMexExchangeConfiguration configuration, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
            : base(configuration, configuration.WebSocketEndpointUrl, log,
                orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;

        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await Messenger.ConnectAsync();
                await Subscribe();

                var response = await ReadResponse();

                for (var i = 0; i < 10 && (response is UnknownResponse || response is SuccessResponse); i++)
                {
                    response = await ReadResponse();
                }

                while (!CancellationToken.IsCancellationRequested)
                {
                    await HandleTableResponse((TableResponse)response);
                    response = await ReadResponse();
                }
            }
            finally
            {
                try
                {
                    await Messenger.StopAsync();
                }
                catch (Exception)
                {

                }
            }
        }

        private async Task<object> ReadResponse()
        {
            var rs = await Messenger.GetResponseAsync();
            var response = JObject.Parse(rs);
            var firstNodeName = response.First.Path;
            if (firstNodeName == ErrorResponse.Token)
            {
                var error = response.ToObject<ErrorResponse>();
                throw new InvalidOperationException(error.Error); // Some domain error. Unable to handle it here
            }

            if (firstNodeName == SuccessResponse.Token)
            {
                return response.ToObject<SuccessResponse>();
            }

            if (firstNodeName == TableResponse.Token)
            {
                return response.ToObject<TableResponse>();
            }

            return new UnknownResponse();
        }

        private async Task HandleTableResponse(TableResponse table)
        {
            var orderBookItems = table.Data.Select(o => o.ToOrderBookItem()).ToList();
            switch (table.Action)
            {
                case Action.Partial:
                    await HandleOrdebookSnapshotAsync(table.Attributes.Symbol,
                        DateTime.UtcNow, // TODO: Use server's date
                        orderBookItems);
                    break;
                case Action.Update:
                case Action.Insert:
                case Action.Delete:
                    await HandleOrdersEventsAsync(table.Attributes.Symbol,
                        ActionToOrderBookEventType(table.Action), orderBookItems);
                    break;
                default:
                    await Log.WriteWarningAsync(nameof(HandleTableResponse), "Parsing table response", 
                        $"Unknown table action {table.Action}");
                    break;
            }
        }

        private OrderBookEventType ActionToOrderBookEventType(
            TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action action)
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

        private async Task Subscribe()
        {
            var filter = _configuration.Instruments.Select(i => new Tuple<string, string>("orderBookL2", 
                BitMexModelConverter.ConvertSymbolFromLykkeToBitMex(i, CurrencyMappingProvider))).ToArray();
            var request = SubscribeRequest.BuildRequest(filter);
            await Messenger.SendRequestAsync(request);
        }
    }
}
