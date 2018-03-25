using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model;
using Lykke.ExternalExchangesApi.Shared;
using Newtonsoft.Json;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.WebSockets;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexWebSocketSubscriber : WebSocketSubscriber, IBitfinexWebSocketSubscriber
    {
        private readonly BitfinexExchangeConfiguration _configuration;
        private readonly List<Func<dynamic, Task>> _handlers = new EditableList<Func<dynamic, Task>>();
        private readonly bool _authenticate;
        private readonly ILog _log;
        private readonly TimeSpan _pingPeriod = TimeSpan.FromSeconds(5);
        private readonly Timer _pingTimer;


        public BitfinexWebSocketSubscriber(BitfinexExchangeConfiguration configuration, bool authenticate, ILog log, TimeSpan? heartbeatPeriod = null) : base(new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log), log, heartbeatPeriod)
        {
            _configuration = configuration;
            _authenticate = authenticate;
            _log = log;
            _pingTimer = new Timer(SendPing);
        }

        public Task Subscribe(Func<dynamic, Task> handlerFunc)
        {
            _handlers.Add(handlerFunc);
            return Task.CompletedTask;
        }

        protected override async Task Connect(CancellationToken token)
        {
            if (_authenticate && !string.IsNullOrEmpty(_configuration.ApiKey) && !string.IsNullOrEmpty(_configuration.ApiSecret))
            {
                if (string.IsNullOrEmpty(_configuration.ApiKey) || string.IsNullOrEmpty(_configuration.ApiSecret))
                {
                    const string error = "ApiKey and ApiSecret must be specified to authenticate the Bitfinex web socket subscription.";
                    await Log.WriteFatalErrorAsync(nameof(Connect), nameof(Connect), new AuthenticationException(error));
                    throw new AuthenticationException(error);
                }

                await base.Connect(token);
                await Authenticate(token);
            }
            else
            {
                await base.Connect(token);
            }
        }

        private async void SendPing(object state)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, CancellationToken))
                {
                    await Messenger.SendRequestAsync(new PingRequest(), linkedCts.Token);
                    RechargePingPong();
                }
            }
            catch (Exception e)
            {
                await _log.WriteWarningAsync(nameof(SendPing), "Unable to send a ping", e.Message);
            }
        }

        private void RechargePingPong()
        {
            _pingTimer.Change(_pingPeriod, Timeout.InfiniteTimeSpan);

        }

        private Task Authenticate(CancellationToken token)
        {
            var request = AuthintificateRequest.BuildRequest(_configuration.ApiKey, _configuration.ApiSecret);
            return Messenger.SendRequestAsync(request, token);
        }

        protected override async Task HandleResponse(string json, CancellationToken token)
        {
            dynamic msg = null;
            try
            {
                msg = EventResponse.Parse(json) ?? (dynamic)HeartbeatResponse.Parse(json) ?? TradeExecutionUpdate.Parse(json);

            }
            catch (JsonSerializationException)
            {
                await _log.WriteWarningAsync(nameof(HandleResponse), "Unexpected message", json);
            }
            if (msg != null)
            {
                await HandleResponse(msg);
                RechargePingPong();
            }

        }

#pragma warning disable S1172 // Unused method parameters should be removed
        private Task<bool> HandleResponse(PongResponse pong)
#pragma warning restore S1172 // Unused method parameters should be removed
        {
            return Task.FromResult(false);
        }

        private async Task<bool> HandleResponse(HeartbeatResponse hbResponse)
        {
            await Log.WriteInfoAsync("Heartbeat", "Heartbeat", $"Heartbeat for channel {hbResponse.ChannelId} received");
            return false;
        }

        private async Task<bool> HandleResponse(AuthMessageResponse message)
        {
            if (message.Code == Code.InvalidApiKey)
            {
                throw new AuthenticationException(message.Message);
            }
            await Log.WriteInfoAsync("Authentication", "Authenticated", $"UserId Id {message.UserId}");
            return false;
        }

        private async Task<bool> HandleResponse(dynamic other)
        {
            foreach (var handler in _handlers)
            {
                await handler(other);
            }
            return true;
        }

        private async Task<bool> HandleResponse(InfoResponse info)
        {
            await Log.WriteInfoAsync("Connecting to Bitfinex", "Connected", $"Protocol version {info.Version}");
            return false;
        }

        private static Task HandleResponse(ErrorEventMessageResponse response)
        {
            throw new InvalidOperationException($"Event: {response.Event} Code: {response.Code} Message: {response.Message}");
        }

        private async Task HandleResponse(EventMessageResponse response)
        {
            await Log.WriteInfoAsync(nameof(HandleResponse), "An event message from bitfinex", $"Event: {response.Event} Code: {response.Code} Message: {response.Message}");
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
    }
}
