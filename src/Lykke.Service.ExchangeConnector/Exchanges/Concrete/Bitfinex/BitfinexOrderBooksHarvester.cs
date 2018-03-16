using Common.Log;
using Lykke.ExternalExchangesApi.Exceptions;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model;
using Lykke.ExternalExchangesApi.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexOrderBooksHarvester : OrderBooksWebSocketHarvester<object, string>
    {
        private readonly BitfinexExchangeConfiguration _configuration;
        private readonly Dictionary<long, Channel> _channels;
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IBitfinexApi _exchangeApi;

        public BitfinexOrderBooksHarvester(BitfinexExchangeConfiguration configuration,
            IHandler<OrderBook> orderBookHandler,
            IHandler<TickPrice> tickPriceHandler,
            ILog log)
        : base(BitfinexExchange.Name, configuration, new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log), log, orderBookHandler)
        {
            _configuration = configuration;
            _channels = new Dictionary<long, Channel>();
            _tickPriceHandler = tickPriceHandler;
            var credenitals = new BitfinexServiceClientCredentials(configuration.ApiKey, configuration.ApiSecret);
            _exchangeApi = new BitfinexApi(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl)
            };
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
                    RechargeHeartbeat();
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
                    // Nothing to do here
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
            var instrumentsToSubscribeFor = _configuration.SupportedCurrencySymbols?
                                  .Select(s => s.ExchangeSymbol)
                                  .ToList() ??  new List<string>();

            if (_configuration.UseSupportedCurrencySymbolsAsFilter == false)
            {
                var response = await _exchangeApi.GetAllSymbols(CancellationToken);
                if (response is Error error)
                {
                    throw new ApiException(error.Message);
                }
                var instrumentsFromExchange = ((IReadOnlyList<string>)response).Select(i=>i.ToUpper()).ToList();

                foreach (var exchInstr in instrumentsFromExchange)
                {
                    if (!instrumentsToSubscribeFor.Contains(exchInstr))
                    {
                        instrumentsToSubscribeFor.Add(exchInstr);
                    }
                }
            }

            if (!instrumentsToSubscribeFor.Any())
            {
                await Log.WriteWarningAsync(nameof(Subscribe), "Subscribing for orderbooks", "Instruments list is empty - its either not set in config and UseSupportedCurrencySymbolsAsFilter is set to true or exchange returned empty symbols list. No symbols to subscribe for.");
            }

            await SubscribeToOrderBookAsync(instrumentsToSubscribeFor);
            await SubscribeToTickerAsync(instrumentsToSubscribeFor);
        }

        private async Task SubscribeToOrderBookAsync(IEnumerable<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                var request = SubscribeOrderBooksRequest.BuildRequest(instrument, "F1", "R0");
                await Messenger.SendRequestAsync(request, CancellationToken);
                var response = await GetResponse();
                await HandleResponse(response);
            }
        }

        private async Task SubscribeToTickerAsync(IEnumerable<string> instruments)
        {
            foreach (var instrument in instruments)
            {
                var request = SublscribeTickeRequest.BuildRequest(instrument);
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
            await Log.WriteInfoAsync(nameof(HandleResponse), "Subscribing on the order book", $"Event: {response.Event} Channel: {response.Channel} Pair: {response.Pair}");

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
            await Log.WriteInfoAsync(nameof(HandleResponse), $"Bitfinex channel {_channels[heartbeat.ChannelId].Pair} heartbeat", string.Empty);
        }

        private async Task HandleResponse(OrderBookSnapshotResponse snapshot)
        {
            var pair = _channels[snapshot.ChannelId].Pair;

            await HandleOrderBookSnapshotAsync(pair,
                DateTime.UtcNow, // TODO: Get this from the server
                snapshot.Orders.Select(BitfinexModelConverter.ToOrderBookItem));
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
            var orderBookItem = BitfinexModelConverter.ToOrderBookItem(response);
            var pair = _channels[response.ChannelId].Pair;
            response.Pair = pair;

            if (response.Price == 0)
            {
                await HandleOrdersEventsAsync(response.Pair,
                    OrderBookEventType.Delete, new[] { orderBookItem });
            }
            else
            {
                await HandleOrdersEventsAsync(response.Pair,
                    OrderBookEventType.Add, new[] { orderBookItem });
            }
        }

        private Task CallTickPricesHandlers(TickPrice tickPrice)
        {
            return _tickPriceHandler.Handle(tickPrice);
        }

        private sealed class Channel
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
