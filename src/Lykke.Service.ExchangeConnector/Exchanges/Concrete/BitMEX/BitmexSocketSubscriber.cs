using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.WebSockets;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitmexSocketSubscriber : WebSocketSubscriber, IBitmexSocketSubscriber
    {
        private readonly bool _authorized;
        private readonly BitMexExchangeConfiguration _configuration;
        private readonly Dictionary<BitmexTopic, Func<TableResponse, Task>> _handlers = new Dictionary<BitmexTopic, Func<TableResponse, Task>>();
        private readonly Timer _pingTimer;
        private const string Ping = "ping";
        private const string Pong = "pong";
        private readonly TimeSpan _pingPeriod = TimeSpan.FromSeconds(5);
        private readonly ILog _log;

        public BitmexSocketSubscriber(IMessenger<object, string> messenger, BitMexExchangeConfiguration configuration, ILog log, bool authorized = false)
            : base(messenger, log)
        {
            _authorized = authorized;
            _configuration = configuration;
            _log = log.CreateComponentScope(nameof(BitmexSocketSubscriber));
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

        public void Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler)
        {
            _handlers[topic] = topicHandler;
        }

        private void RechargePingPong()
        {
            _pingTimer.Change(_pingPeriod, Timeout.InfiniteTimeSpan);

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

        }

        protected override async Task HandleResponse(string json, CancellationToken token)
        {
            if (json == Pong)
            {
                RechargePingPong();
                return;
            }
            var response = JObject.Parse(json);

            var infoProp = response.GetValue("info", StringComparison.InvariantCultureIgnoreCase);
            var errorProp = response.GetValue("error", StringComparison.InvariantCultureIgnoreCase);
            var successProp = response.GetValue("success", StringComparison.InvariantCultureIgnoreCase);
            var tableProp = response.GetValue("table", StringComparison.InvariantCultureIgnoreCase);

            if (errorProp != null)
            {
                var error = response.ToObject<ErrorResponse>();
                if (error.Status.HasValue && error.Status.Value == (int)HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException(error.Error);
                }
                throw new InvalidOperationException(error.Error);
            }

            if (infoProp != null || successProp != null && successProp.Value<bool>())
            {
                // Ignoring success and info messages
            }
            else if (tableProp != null)
            {
                await HandleTableResponse(response.ToObject<TableResponse>());
                RechargePingPong();
            }
            else
            {
                // Unknown response
                await Log.WriteWarningAsync(nameof(HandleResponse), "", $"Ignoring unknown response: {json}");
            }

        }

        protected virtual Task Authorize(CancellationToken token)
        {
            var credenitals = new BitMexServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret);

            var request = new AuthRequest
            {
                Operation = "authKey",
                Arguments = credenitals.BuildAuthArguments("GET/realtime")
            };

            return Messenger.SendRequestAsync(request, token);
        }

        private Task Subscribe(IEnumerable<BitmexTopic> topics, CancellationToken token)
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
                && Enum.TryParse(typeof(BitmexTopic), resp.Table, true, out var table)
                && _handlers.TryGetValue((BitmexTopic)table, out var respHandler))
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

        public override void Stop()
        {
            _pingTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            base.Stop();
        }
    }
}
