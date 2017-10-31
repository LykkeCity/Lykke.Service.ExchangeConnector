using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Exchanges.Concrete.Gemini.RestClient;
using TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities;
using TradingBot.Exchanges.Concrete.Gemini.WssClient;
using TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Gemini
{
    internal sealed class GeminiExchange : Exchange
    {
        public new static readonly string Name = "Gemini";
        private static readonly string _geminiExchangeTypeName = nameof(GeminiExchange);

        private readonly GeminiExchangeConfiguration _configuration;
        private readonly IGeminiRestApi _restApi;
        private readonly GeminiWebSocketApi _websocketApi;
        private readonly GeminiConverters _converters;
        private CancellationTokenSource _webSocketCtSource;

        public GeminiExchange(GeminiExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, ILog log) 
            : base(Name, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            _converters = new GeminiConverters(configuration, Name);

            _restApi = CreateRestApiClient();
            _websocketApi = CreateWebSocketsApiClient();
        }

        private GeminiRestApi CreateRestApiClient()
        {
            return new GeminiRestApi(_configuration.ApiKey, _configuration.ApiSecret,
                _configuration.PassPhrase)
            {
                BaseUri = new Uri(_configuration.RestEndpointUrl),
                ConnectorUserAgent = _configuration.UserAgent
            };
        }

        private GeminiWebSocketApi CreateWebSocketsApiClient()
        {
            var websocketApi = new GeminiWebSocketApi(_configuration.ApiKey, _configuration.ApiSecret,
                _configuration.PassPhrase)
            {
                BaseUri = new Uri(_configuration.WssEndpointUrl)
            };
            websocketApi.Ticker += OnWebSocketTicker;
            websocketApi.OrderReceived += OnWebSocketOrderReceived;
            websocketApi.OrderChanged += OnOrderChanged;
            websocketApi.OrderDone += OnWebSocketOrderDone;

            return websocketApi;
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, 
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = _converters.LykkeSymbolToGeminiSymbol(instrument.Name);
            var orderType = _converters.OrderTypeToGeminiOrderType(signal.OrderType);
            var side = _converters.TradeTypeToGeminiOrderSide(signal.TradeType);
            var volume = signal.Volume;
            var price = (!signal.Price.HasValue || signal.Price == 0 ) ? 1 : signal.Price.Value;
            var cts = CreateCancellationTokenSource(timeout);

            try
            {
                var response = await _restApi.AddOrder(symbol, volume, price, side, orderType, cts.Token, 
                    (sender, httpRequest) => OnSentHttpRequest(sender, httpRequest, translatedSignal), 
                    (sender, httpResponse) => OnReceivedHttpRequest(sender, httpResponse, translatedSignal));
                var trade = _converters.OrderToTrade(response);
                return trade;
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, 
            TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            if (!Guid.TryParse(signal.OrderId, out var id))
                throw new ApiException($"{Name} order id can be only Guid");

            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var response = await _restApi.CancelOrder(id, cts.Token,
                    (sender, httpRequest) => OnSentHttpRequest(sender, httpRequest, translatedSignal),
                    (sender, httpResponse) => OnReceivedHttpRequest(sender, httpResponse, translatedSignal));
                if (response == null || response.Count == 0)
                    return null;

                return null;  // TODO: Here we should just return the ID of the cancelled order 
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override async Task<ExecutedTrade> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            if (!Guid.TryParse(id, out var orderId))
                throw new ApiException($"{Name} order id can be only Guid");

            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var response = await _restApi.GetOrderStatus(orderId, cts.Token);
                var trade = _converters.OrderToTrade(response);
                return trade;
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override async Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var response = await _restApi.GetOpenOrders(cts.Token);
                var trades = response.Select(_converters.OrderToTrade);
                return trades;
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override async Task<IEnumerable<AccountBalance>> GetAccountBalance(TimeSpan timeout)
        {
            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var exchangeBalances = await _restApi.GetBalances(cts.Token);
                var accountBalances = exchangeBalances.Select(_converters.GeminiBalanceToAccountBalance);

                return accountBalances;
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout)
        {
            throw new NotSupportedException(); 
        }

        public override Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        protected override async void StartImpl()
        {
            _webSocketCtSource = new CancellationTokenSource();

            try
            {
                await _websocketApi.ConnectAsync(_webSocketCtSource.Token);
                OnConnected();

                await _websocketApi.SubscribeToPrivateUpdatesAsync(Instruments.Select(i => i.Name).ToList(), 
                    _webSocketCtSource.Token);
            }
            catch (Exception ex)
            {
                await LogAsync(ex);
            }
        }

        protected override async void StopImpl()
        {
            try
            {
                if (_webSocketCtSource != null)
                _webSocketCtSource.Cancel();

                await _websocketApi.CloseConnectionAsync(CancellationToken.None);
                OnStopped();
            }
            catch (Exception ex)
            {
                await LogAsync(ex);
            }
        }

        private static CancellationTokenSource CreateCancellationTokenSource(TimeSpan timeout)
        {
            return timeout == TimeSpan.Zero
                ? new CancellationTokenSource()
                : new CancellationTokenSource(timeout);
        }

        private async void OnSentHttpRequest(object sender, SentHttpRequest request, 
            TranslatedSignalTableEntity translatedSignal)
        {
            var url = request.Uri.ToString();
            translatedSignal?.RequestSent(request.HttpMethod, url, request.Content);
            await LogAsync($"Making request to url: {url}. {translatedSignal?.RequestSentToExchange}");
        }

        private void OnReceivedHttpRequest(object sender, ReceivedHttpResponse response, 
            TranslatedSignalTableEntity translatedSignal)
        {
            translatedSignal?.ResponseReceived(response.Content);
        }

        private void OnWebSocketTicker(object sender, GeminiWssTicker ticker)
        {
            var tickPrice = new TickPrice(ticker.Time, ticker.BestAsk, ticker.BestBid);
            CallTickPricesHandlers(new InstrumentTickPrices(
                new Instrument(Name, ticker.ProductId), new[] { tickPrice }));
        }

        private void OnWebSocketOrderReceived(object sender, GeminiWssOrderReceived order)
        {
            new ExecutedTrade(new Instrument(Name, order.ProductId),
                order.Time, order.Price ?? 0, order.Size,
                order.Side == GeminiOrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                order.OrderId.ToString(), ExecutionStatus.New);
        }

        private void OnOrderChanged(object sender, GeminiWssOrderChange order)
        {
            new ExecutedTrade(new Instrument(Name, order.ProductId),
                order.Time, order.Price ?? 0, order.NewSize,
                order.Side == GeminiOrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                order.OrderId.ToString(),
                ExecutionStatus.PartialFill);
        }

        private void OnWebSocketOrderDone(object sender, GeminiWssOrderDone order)
        {
            new ExecutedTrade(new Instrument(Name, order.ProductId),
                order.Time, order.Price ?? 0, order.RemainingSize,
                order.Side == GeminiOrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                order.OrderId.ToString(), 
                order.Reason == "cancelled" ? ExecutionStatus.Cancelled : ExecutionStatus.Fill);
        }

        private async Task LogAsync(string message, [CallerMemberName]string context = null)
        {
            const int maxMessageLength = 32000;

            if (LykkeLog == null)
                return;

            if (message.Length >= maxMessageLength)
                message = message.Substring(0, maxMessageLength);

            await LykkeLog.WriteInfoAsync(_geminiExchangeTypeName, _geminiExchangeTypeName, context, message);
        }

        private async Task LogAsync(Exception ex, [CallerMemberName]string context = null)
        {
            if (LykkeLog == null)
                return;

            await LykkeLog.WriteErrorAsync(_geminiExchangeTypeName, _geminiExchangeTypeName, context, ex);
        }
    }
}
