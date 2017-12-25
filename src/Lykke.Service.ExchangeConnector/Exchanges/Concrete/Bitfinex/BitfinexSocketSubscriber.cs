using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model;
using Lykke.ExternalExchangesApi.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.WebSockets;
using SubscribeRequest = Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model.SubscribeRequest;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitfinexSocketSubscriber : WebSocketSubscriber, IBitfinexSocketSubscriber
    {
        private readonly bool _authorized;
        private readonly BitMexExchangeConfiguration _configuration;
        private readonly ILog _log;
        private readonly Dictionary<WsChannel, Func<object, Task>> _handlers = new Dictionary<WsChannel, Func<object, Task>>();
        private readonly Timer _pingTimer;
        private const string Ping = "ping";
        private const string Pong = "pong";
        private readonly TimeSpan _pingPeriod = TimeSpan.FromSeconds(5);
        private readonly Dictionary<long, Channel> _channels;


        public BitfinexSocketSubscriber(IMessenger<object, string> messenger, BitMexExchangeConfiguration configuration, ILog log, bool authorized = false)
            : base(messenger, log)
        {
            _authorized = authorized;
            _configuration = configuration;
            _log = log.CreateComponentScope(nameof(BitfinexSocketSubscriber));
            _pingTimer = new Timer(SendPing);
        }

        private async void SendPing(object state)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                {
                    await Messenger.SendRequestAsync(Ping, cts.Token);
                    RechargePingPong();
                }
            }
            catch (Exception e)
            {
                await _log.WriteWarningAsync(nameof(SendPing), "Unable to send a ping", e.Message);
            }
        }

        public void Subscribe(WsChannel topic, Func<object, Task> topicHandler)
        {
            _handlers[topic] = topicHandler;
        }

        protected override async Task Connect(CancellationToken token)
        {
            if (_authorized && (string.IsNullOrEmpty(_configuration.ApiKey) || string.IsNullOrEmpty(_configuration.ApiSecret)))
            {
                var error = "ApiKey and ApiSecret must be specified to authorize BitMex web socket subscription.";
                await Log.WriteFatalErrorAsync(nameof(BitmexSocketSubscriber), nameof(Connect), new InvalidOperationException(error));
                throw new AuthenticationException(error);
            }

            await base.Connect(token);
            if (_authorized)
            {
                await Authorize(token);
            }
            await Subscribe(_handlers.Keys, token);
            RechargePingPong();
        }

        public override void Stop()
        {
            _pingTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            base.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _pingTimer.Dispose();
        }

        private void RechargePingPong()
        {
            _pingTimer.Change(_pingPeriod, Timeout.InfiniteTimeSpan);

        }

        protected override Task HandleResponse(string json, CancellationToken token)
        {
            RechargePingPong();

            if (json == Pong)
            {
                return;
            }
            var result = EventResponse.Parse(json) ??
                         TickerResponse.Parse(json) ??
                         OrderBookSnapshotResponse.Parse(json) ??
                         (dynamic)OrderBookUpdateResponse.Parse(json) ??
                         HeartbeatResponse.Parse(json);
        }

        private Task Authorize(CancellationToken token)
        {
            var credenitals = new BitMexServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret);

            var request = new AuthRequest
            {
                Operation = "authKey",
                Arguments = credenitals.BuildAuthArguments("GET/realtime")
            };

            return Messenger.SendRequestAsync(request, token);
        }

        private Task Subscribe(IEnumerable<WsChannel> topics, CancellationToken token)
        {
            var filter = new List<Tuple<string, string>>();
            foreach (var topic in topics)
            {
                filter.AddRange(
                    _configuration.SupportedCurrencySymbols.Select(i => new Tuple<string, string>(topic.ToString(), i.ExchangeSymbol)));
            }

            var request = SubscribeRequest.BuildRequest(filter.ToArray());

            return Messenger.SendRequestAsync(request, token);
        }

        private async Task HandleTableResponse(TableResponse resp)
        {
            if (!string.IsNullOrEmpty(resp.Table)
                && Enum.TryParse(typeof(WsChannel), resp.Table, true, out var table)
                && _handlers.TryGetValue((WsChannel)table, out var respHandler))
            {
                try
                {
                    await respHandler(resp);
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(HandleTableResponse), $"An exception occurred while handling message: '{JsonConvert.SerializeObject(resp)}'", ex);
                }
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
