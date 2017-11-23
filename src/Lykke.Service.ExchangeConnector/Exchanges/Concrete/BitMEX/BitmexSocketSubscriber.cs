using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.WebSockets;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    class BitmexSocketSubscriber : WebSocketSubscriber
    {
        protected readonly BitMexExchangeConfiguration _configuration;
        private readonly Dictionary<string, Func<TableResponse, Task>> _handlers = new Dictionary<string, Func<TableResponse, Task>>();

        public BitmexSocketSubscriber(BitMexExchangeConfiguration configuration, ILog log)
            : base(configuration.WebSocketEndpointUrl, log)
        {
            _configuration = configuration;
        }

        public virtual BitmexSocketSubscriber Subscribe(BitmexTopic topic, Func<TableResponse, Task> handler)
        {
            _handlers[topic.ToString().ToLowerInvariant()] = handler;
            return this;
        }

        protected override async Task Connect(CancellationToken token)
        {
            await base.Connect(token);
            if (!String.IsNullOrEmpty(_configuration.ApiSecret) && !string.IsNullOrEmpty(_configuration.ApiSecret))
            {
                await Authorize(token);
            }
            await Subscribe(token);
        }

        protected override async Task HandleResponse(string json, CancellationToken token)
        {
            var response = JObject.Parse(json);

            var firstNodeName = response.First.Path;
            if (firstNodeName == ErrorResponse.Token)
            {
                var error = response.ToObject<ErrorResponse>();
                throw new InvalidOperationException(error.Error); // Some domain error. Unable to handle it here
            }
            else if (firstNodeName == SuccessResponse.Token)
            {
                // ignore
            }
            else if (firstNodeName == TableResponse.Token)
            {
                TableResponse table = response.ToObject<TableResponse>();
                await HandleTableResponse(table);
            }
            else
            {
                // UnknownResponse
            }
        }

        protected virtual async Task Authorize(CancellationToken token)
        {
            var credenitals = new BitMexServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret);

            var request = new AuthRequest
            {
                Operation = "authKey",
                Arguments = credenitals.BuildAuthArguments("GET/realtime")
            };

            await Messenger.SendRequestAsync(request, token);
        }

        protected virtual async Task Subscribe(CancellationToken token)
        {
            var bitMexInstruments = _configuration.SupportedCurrencySymbols;

            var filter = bitMexInstruments.Select(i => new Tuple<string, string>("orderBookL2", i.ExchangeSymbol))
                    .Concat(bitMexInstruments.Select(i => new Tuple<string, string>("quote", i.ExchangeSymbol)))
                    .Concat(bitMexInstruments.Select(i => new Tuple<string, string>("order", i.ExchangeSymbol)))
                    .ToArray();
            var request = SubscribeRequest.BuildRequest(filter);

            await Messenger.SendRequestAsync(request, token);
        }

        private async Task HandleTableResponse(TableResponse resp)
        {
            if (!string.IsNullOrEmpty(resp.Table))
            {
                var table = resp.Table.ToLowerInvariant();
                Func<TableResponse, Task> handler;
                if (_handlers.TryGetValue(table, out handler))
                {
                    try
                    {
                        await handler(resp);
                    }
                    catch (Exception ex)
                    {
                        await Log.WriteErrorAsync(nameof(HandleTableResponse), $"An exception occurred while handling message: '{JsonConvert.SerializeObject(resp)}'", ex);
                    }
                }
            }
        }
    }
}
