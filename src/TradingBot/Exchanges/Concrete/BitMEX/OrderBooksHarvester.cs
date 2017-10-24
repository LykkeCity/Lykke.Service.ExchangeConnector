using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Polly;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class OrderBooksHarvester
    {
        private readonly BitMexExchangeConfiguration _configuration;
        private readonly ILog _log;
        private readonly WebSocketTextMessenger _messenger;
        private bool _stopRequested;
        private readonly HashSet<RowItem> _orderBookSnapshot;
        private Task _messageLoopTaks;

        public OrderBooksHarvester(BitMexExchangeConfiguration configuration, ILog log)
        {
            _configuration = configuration;
            _log = log.CreateComponentScope(nameof(OrderBooksHarvester));
            _messenger = new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, _log);
            _orderBookSnapshot = new HashSet<RowItem>();
        }

        private JsonSerializerSettings SerializationSettings { get; } = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter>
            {
                new Iso8601TimeSpanConverter()
            }
        };

        private Func<OrderBook, Task> _newOrderBookHandler;

        public void AddHandler(Func<OrderBook, Task> handler)
        {
            _newOrderBookHandler = handler;
        }

        public void Start()
        {
            _messageLoopTaks = new Task(MessageLoop);
            _messageLoopTaks.Start();
        }

        public void Stop()
        {
            _stopRequested = true;
        }

        private async void MessageLoop()
        {
            const int attempts = int.MaxValue;
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(attempts, attempt => TimeSpan.FromMinutes(30)); // In case the exchange outage

            try
            {
                await retryPolicy.ExecuteAsync(async () => await MessageLoopImpl());
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(MessageLoopImpl), "Unable to connect to BitMex. Silence for ever...", ex);
            }
        }

        private async Task MessageLoopImpl()
        {
            await _log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");

            try
            {
                while (!_stopRequested)
                {
                    await _messenger.Connect();
                    await SubscribeOnOrderBooks();

                    var response = await ReadResponse();

                    for (var i = 0; i < 10 && (response is UnknownResponse || response is SuccessResponse); i++)
                    {
                        response = await ReadResponse();
                    }

                    try
                    {
                        while (!_stopRequested)
                        {
                            await HandleTableResponse((TableResponse)response);
                            response = await ReadResponse();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await _log.WriteErrorAsync(nameof(MessageLoopImpl), "An exception occurred while receiving order books. Try to recover", ex);
                    }
                    try
                    {
                        await _messenger.Stop();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(MessageLoopImpl), "An exception occurred while working with WebSocket. Unable to recover", ex);
            }

            await _log.WriteInfoAsync(nameof(MessageLoopImpl), "Exiting message loop", "");
        }

        private async Task<object> ReadResponse()
        {
            var responseText = await _messenger.GetResponse();

            if (responseText.Contains(ErrorResponse.Token))
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(responseText, SerializationSettings);
                throw new InvalidOperationException(error.Error); // Some domain error. Unable to handle it here
            }

            if (responseText.Contains(SuccessResponse.Token))
            {
                return JsonConvert.DeserializeObject<SuccessResponse>(responseText, SerializationSettings);
            }

            if (responseText.Contains(TableResponse.Token))
            {
                return JsonConvert.DeserializeObject<TableResponse>(responseText, SerializationSettings);
            }

            return new UnknownResponse();
        }

        private async Task HandleTableResponse(TableResponse table)
        {
            switch (table.Action)
            {
                case Action.Partial:
                    _orderBookSnapshot.Clear();
                    goto case Action.Update;
                case Action.Update:
                case Action.Insert:
                    foreach (var item in table.Data)
                    {
                        _orderBookSnapshot.Add(item);
                    }
                    break;
                case Action.Delete:
                    foreach (var item in table.Data)
                    {
                        _orderBookSnapshot.Remove(item);
                    }
                    break;
                default:
                    await _log.WriteWarningAsync(nameof(HandleTableResponse), "Parsing table response", $"Unknown table action {table.Action}");
                    break;
            }

            var orderBooks = ConvertSnapshot();

            foreach (var orderBook in orderBooks)
            {
                await _newOrderBookHandler(orderBook);
            }
        }


        private IReadOnlyCollection<OrderBook> ConvertSnapshot()
        {
            var orderBooks = from si in _orderBookSnapshot
                             group si by si.Symbol into g
                             let asks = g.Where(i => i.Side == Side.Sell).Select(i => new VolumePrice(i.Price, i.Size)).ToArray()
                             let bids = g.Where(i => i.Side == Side.Buy).Select(i => new VolumePrice(i.Price, i.Size)).ToArray()
                             let assetPair = BitMexModelConverter.ConvertSymbolFromBitMexToLykke(g.Key, _configuration)
                             select new OrderBook(BitMexExchange.Name, g.Key, asks, bids, DateTime.UtcNow);
            return orderBooks.ToArray();
        }


        private async Task SubscribeOnOrderBooks()
        {
            var filter = _configuration.Instruments.Select(i => new Tuple<string, string>("orderBookL2", BitMexModelConverter.ConvertSymbolFromLykkeToBitMex(i, _configuration))).ToArray();
            var request = SubscribeRequest.BuildRequest(filter);
            await _messenger.SendRequest(request);
        }
    }
}
