using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using SubscribeRequest = TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model.SubscribeRequest;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexOrderBooksHarvester : OrderBooksWebSocketHarvester<object, string>
    {
        private readonly BitfinexExchangeConfiguration _configuration;
        private readonly Dictionary<long, Channel> _channels;
        private readonly List<Func<TickPrice, Task>> _tickPriceHandlers;

        public BitfinexOrderBooksHarvester(string exchangeName, BitfinexExchangeConfiguration configuration, ILog log, OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
        : base(exchangeName, configuration, new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log), log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;
            _channels = new Dictionary<long, Channel>();
            _tickPriceHandlers = new List<Func<TickPrice, Task>>();
        }

        public void AddTickPriceHandler(Func<TickPrice, Task> handler)
        {
            _tickPriceHandlers.Add(handler);
        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await Messenger.ConnectAsync(CancellationToken);
                await Subscribe();
                RechargeHeartbeat();
                while (!CancellationToken.IsCancellationRequested)
                {
                    var resp = await GetResponse();
                    await HandleResponse(resp);
                }
            }
            finally
            {
                try
                {
                    await Messenger.StopAsync(CancellationToken);
                }
                catch
                {
                }
            }
        }

        private async Task<dynamic> GetResponse()
        {
            var json = await Messenger.GetResponseAsync(CancellationToken);

            var result = EventResponse.Parse(json) ?? 
                TickerResponse.Parse(json) ??
                OrderBookSnapshotResponse.Parse(json) ?? 
                (dynamic)OrderBookUpdateResponse.Parse(json) ??
                HeartbeatResponse.Parse(json);
            return result;
        }

        private async Task Subscribe()
        {
            var instruments = _configuration.SupportedCurrencySymbols
                .Select(s => s.ExchangeSymbol)
                .ToList();

            await SubscribeToOrderBookAsync(instruments);
            await SubscribeToTickerAsync(instruments);
        }

        private async Task SubscribeToOrderBookAsync(IEnumerable<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                var request = new SubscribeRequest
                {
                    Event = "subscribe",
                    Channel = "book",
                    Pair = instrument,
                    Prec = "R0",
                    Freq = "F0"
                };
                await Messenger.SendRequestAsync(request, CancellationToken);
                var response = await GetResponse();
                await HandleResponse(response);
            }
        }

        private async Task SubscribeToTickerAsync(IEnumerable<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                var request = new SubscribeRequest
                {
                    Event = "subscribe",
                    Channel = "ticker",
                    Pair = instrument
                };
                await Messenger.SendRequestAsync(request, CancellationToken);
                var response = await GetResponse();
                await HandleResponse(response);
            }
        }

        private async Task HandleResponse(InfoResponse response)
        {
            await Log.WriteInfoAsync(nameof(HandleResponse), "Connecting to Bitfinex", $"{response.Event} Version {response.Version}");
        }

        private async Task HandleResponse(SubscribedResponse response)
        {
            await Log.WriteInfoAsync(nameof(HandleResponse), "Subscribing on the order book", $"Event: {response.Event} Pair: {response.Pair}");

            if (!_channels.TryGetValue(response.ChanId, out var channel))
            {
                channel = new Channel(response.ChanId, response.Pair);
                _channels[channel.Id] = channel;
            }
        }

        private async Task HandleResponse(EventMessageResponse response)
        {
            await Log.WriteInfoAsync(nameof(HandleResponse), "Subscribed on the order book", $"Event: {response.Event} Code: {response.Code} Message: {response.Message}");
        }

        private Task HandleResponse(ErrorEventMessageResponse response)
        {
            throw new InvalidOperationException($"Event: {response.Event} Code: {response.Code} Message: {response.Message}");
        }

        private async Task HandleResponse(HeartbeatResponse heartbeat)
        {
            RechargeHeartbeat();
            await Log.WriteInfoAsync(nameof(HandleResponse), $"Bitfinex channel {_channels[heartbeat.ChannelId].Pair} heartbeat", string.Empty);
        }

        private async Task HandleResponse(OrderBookSnapshotResponse snapshot)
        {
            var pair = _channels[snapshot.ChannelId].Pair;

            await HandleOrdebookSnapshotAsync(pair,
                DateTime.UtcNow, // TODO: Get this from the server
                snapshot.Orders.Select(o => o.ToOrderBookItem()));
        }

        private async Task HandleResponse(TickerResponse ticker)
        {
            var pair = _channels[ticker.ChannelId].Pair; 

            var tickPrice = new TickPrice(new Instrument(ExchangeName, pair), DateTime.UtcNow, 
                ticker.Ask, ticker.Bid);
            await CallTickPricesHandlers(tickPrice);
        }

        private async Task HandleResponse(OrderBookUpdateResponse response)
        {
            var orderBookItem = response.ToOrderBookItem();
            var pair = _channels[response.ChannelId].Pair;
            response.Pair = pair;

            if (response.Price == 0)
            {
                await HandleOrdersEventsAsync(response.Pair, 
                    OrderBookEventType.Delete, new[] {orderBookItem});
            }
            else
            {
                await HandleOrdersEventsAsync(response.Pair,
                    OrderBookEventType.Add, new[] { orderBookItem });
            }
        }

        private Task CallTickPricesHandlers(TickPrice tickPrice)
        {
            return Task.WhenAll(_tickPriceHandlers.Select(handler => handler(tickPrice)));
        }

        private class Channel
        {
            public long Id { get; }
            public string Pair { get; }

            public Channel(long id, string pair)
            {
                Id = id;
                Pair = pair;
            }
        }
    }
}
