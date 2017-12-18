using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using Lykke.ExternalExchangesApi.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.WebSockets;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    class BitmexSocketSubscriber : WebSocketSubscriber, IBitmexSocketSubscriber
    {
        private readonly bool _authorized;
        private readonly BitMexExchangeConfiguration _configuration;
        private readonly Dictionary<string, Func<TableResponse, Task>> _handlers = new Dictionary<string, Func<TableResponse, Task>>();

        public BitmexSocketSubscriber(IMessenger<object, string> messenger, BitMexExchangeConfiguration configuration, ILog log, bool authorized = false)
            : base(messenger, log)
        {
            _authorized = authorized;
            _configuration = configuration;
        }

        public virtual IBitmexSocketSubscriber Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler)
        {
            _handlers[topic.ToString().ToLowerInvariant()] = topicHandler;
            return this;
        }

        protected override async Task<Result> Connect(CancellationToken token)
        {
            if (_authorized && (string.IsNullOrEmpty(_configuration.ApiKey) || string.IsNullOrEmpty(_configuration.ApiSecret)))
            {
                var error = "ApiKey and ApiSecret must be specified to authorize BitMex web socket subscription.";
                await Log.WriteFatalErrorAsync(nameof(BitmexSocketSubscriber), nameof(HandleResponse), new InvalidOperationException(error));
                return new Result(isFailure: true, _continue: false, error: error);
            }

            await base.Connect(token);
            if (_authorized)
            {
                await Authorize(token);
                await Subscribe(new[] { "order" }, token);
            }
            await Subscribe(new[] { "orderBookL2", "quote" }, token);
            return Result.Ok;
        }

        protected override async Task<Result> HandleResponse(string json, CancellationToken token)
        {
            var response = JObject.Parse(json);

            JToken infoProp = response.GetValue("info", StringComparison.InvariantCultureIgnoreCase);
            JToken errorProp = response.GetValue("error", StringComparison.InvariantCultureIgnoreCase);
            JToken successProp = response.GetValue("success", StringComparison.InvariantCultureIgnoreCase);
            JToken tableProp = response.GetValue("table", StringComparison.InvariantCultureIgnoreCase);

            if (errorProp != null)
            {
                var error = response.ToObject<ErrorResponse>();
                if ((error.Status.HasValue && error.Status.Value == (int)HttpStatusCode.Unauthorized))
                {
                    await Log.WriteFatalErrorAsync(nameof(BitmexSocketSubscriber), nameof(HandleResponse), new AuthenticationException(error.Error));
                    return new Result(isFailure: true, _continue: false, error: error.Error);
                }
                else
                {
                    throw new InvalidOperationException(error.Error);
                }
            }
            else if (infoProp != null || (successProp != null && successProp.Value<bool>()))
            {
                // Ignoring success and info messages
            }
            else if (tableProp != null)
            {
                await HandleTableResponse(response.ToObject<TableResponse>());
            }
            else
            {
                // Unknown response
                await Log.WriteWarningAsync(nameof(HandleResponse), "", $"Ignoring unknown response: {json}");
            }

            return Result.Ok;
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

        protected virtual async Task Subscribe(IEnumerable<string> topics, CancellationToken token)
        {
            var filter = new List<Tuple<string, string>>();
            foreach (var topic in topics)
            {
                filter.AddRange(
                    _configuration.SupportedCurrencySymbols.Select(i => new Tuple<string, string>(topic, i.ExchangeSymbol)));
            }

            var request = SubscribeRequest.BuildRequest(filter.ToArray());
            await Messenger.SendRequestAsync(request, token);
        }

        private async Task HandleTableResponse(TableResponse resp)
        {
            if (!string.IsNullOrEmpty(resp.Table))
            {
                var table = resp.Table.ToLowerInvariant();
                if (_handlers.TryGetValue(table, out var respHandler))
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
        }
    }
}
