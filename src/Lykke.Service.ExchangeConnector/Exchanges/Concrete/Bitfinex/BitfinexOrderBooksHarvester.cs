using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.BitMEX;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Repositories;
using SubscribeRequest = TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model.SubscribeRequest;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexOrderBooksHarvester : OrderBooksHarvesterBase
    {
        private readonly BitfinexExchangeConfiguration _configuration;
        private readonly Dictionary<long, Channel> _channels;
        private readonly Timer _heartBeatMonitoringTimer;
        private bool _heartIsStoped;

        public BitfinexOrderBooksHarvester(BitfinexExchangeConfiguration configuration, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository) : 
            base(configuration, configuration.WebSocketEndpointUrl, log, 
                orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;
            _channels = new Dictionary<long, Channel>();
            _heartBeatMonitoringTimer = new Timer(state => _heartIsStoped = true);
        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await Messenger.ConnectAsync();
                await Subscribe();
                _heartBeatMonitoringTimer.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
                while (!CancellationToken.IsCancellationRequested)
                {
                    var resp = await GetResponse();
                    await HandleResponse(resp);
                    if (_heartIsStoped)
                    {
                        throw new InvalidOperationException("Did not received heart beat from bitfinex within 10 sec.");
                    }
                }
            }
            finally
            {
                try
                {
                    await Messenger.StopAsync();
                }
                catch
                {
                }
            }
        }

        private async Task<dynamic> GetResponse()
        {
            var json = await Messenger.GetResponseAsync();

            var result = EventResponse.Parse(json) ?? OrderBookSnapshotResponse.Parse(json) ?? (dynamic)OrderBookUpdateResponse.Parse(json) ?? HeartbeatResponse.Parse(json);
            return result;
        }

        private async Task Subscribe()
        {
            var instruments = _configuration.Instruments.Select(i => BitMexModelConverter.ConvertSymbolFromLykkeToBitMex(i, _configuration));

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
                await Messenger.SendRequestAsync(request);
                var response = await GetResponse();
                HandleResponse(response);
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
            _heartIsStoped = false;
            _heartBeatMonitoringTimer.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
            await Log.WriteInfoAsync(nameof(HandleResponse), $"Bitfinex channel {_channels[heartbeat.ChannelId].Pair} heartbeat", string.Empty);
        }

        private async Task HandleResponse(OrderBookSnapshotResponse snapshot)
        {
            var pair = _channels[snapshot.ChannelId].Pair;

            await HandleOrdebookSnapshotAsync(pair,
                DateTime.UtcNow, // TODO: Get this from the server
                snapshot.Orders.Select(o => o.ToOrderBookItem()));
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
