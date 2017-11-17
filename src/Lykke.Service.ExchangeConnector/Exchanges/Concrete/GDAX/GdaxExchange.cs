using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Polly;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;
using TradingBot.Exchanges.Concrete.GDAX.WssClient;
using TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal class GdaxExchange : Exchange
    {
        public new static readonly string Name = "GDAX";
        private static readonly string _gdaxExchangeTypeName = nameof(GdaxExchange);

        private readonly GdaxExchangeConfiguration _configuration;
        private readonly IGdaxRestApi _restApi;
        private readonly GdaxWebSocketApi _websocketApi;
        private readonly GdaxConverters _converters;
        private CancellationTokenSource _webSocketCtSource;
        private readonly GdaxOrderBooksHarvester _orderBooksHarvester;

        public GdaxExchange(GdaxExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, 
            GdaxOrderBooksHarvester orderBookHarvester, ILog log) 
            : base(Name, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            _converters = new GdaxConverters(configuration.SupportedCurrencySymbols, Name);

            _orderBooksHarvester = orderBookHarvester;
            _orderBooksHarvester.ExchangeName = Name;
            _orderBooksHarvester.AddHandler(CallOrderBookHandlers);

            _restApi = CreateRestApiClient();
            _websocketApi = CreateWebSocketsApiClient();
        }

        private GdaxRestApi CreateRestApiClient()
        {
            return new GdaxRestApi(_configuration.ApiKey, _configuration.ApiSecret,
                _configuration.PassPhrase, _configuration.RestEndpointUrl, _configuration.UserAgent);
        }

        private GdaxWebSocketApi CreateWebSocketsApiClient()
        {
            var websocketApi = new GdaxWebSocketApi(LykkeLog, _configuration.ApiKey, 
                _configuration.ApiSecret, _configuration.PassPhrase, _configuration.WssEndpointUrl);
            websocketApi.Ticker += OnWebSocketTickerAsync;
            websocketApi.OrderReceived += OnWebSocketOrderReceivedAsync;
            websocketApi.OrderChanged += OnOrderChangedAsync;
            websocketApi.OrderDone += OnWebSocketOrderDoneAsync;

            return websocketApi;
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal, 
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = _converters.LykkeSymbolToExchangeSymbol(signal.Instrument.Name);
            var orderType = _converters.OrderTypeToGdaxOrderType(signal.OrderType);
            var side = _converters.TradeTypeToGdaxOrderSide(signal.TradeType);
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

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution( 
            TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            if (!Guid.TryParse(signal.OrderId, out var id))
                throw new ApiException("GDAX order id can be only Guid");

            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var response = await _restApi.CancelOrder(id, cts.Token,
                    (sender, httpRequest) => OnSentHttpRequest(sender, httpRequest, translatedSignal),
                    (sender, httpResponse) => OnReceivedHttpRequest(sender, httpResponse, translatedSignal));
                if (!response) 
                    return null;

                return null;  // TODO: Here we should just return boolean result
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override async Task<ExecutedTrade> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            if (!Guid.TryParse(id, out var orderId))
                throw new ApiException("GDAX order id can be only Guid");

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
                var gdaxBalances = await _restApi.GetBalances(cts.Token);
                var accountBalances = gdaxBalances.Select(_converters.GdaxBalanceToAccountBalance);

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
                _orderBooksHarvester.Start();

                var retryPolicy = Policy
                    .Handle<Exception>(ex => !(ex is OperationCanceledException))
                    .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(attempt),
                        (exception, span, attempt, context) => LogAsync(exception, $"Connection attemp #{attempt}"));
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
                _orderBooksHarvester.Stop();

                _webSocketCtSource?.Cancel();

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

        private async Task OnWebSocketTickerAsync(object sender, GdaxWssTicker ticker)
        {
            var tickPrice = new TickPrice(new Instrument(Name, ticker.ProductId), ticker.Time, 
                ticker.BestAsk ?? 0, ticker.BestBid ?? 0);
            await CallTickPricesHandlers(tickPrice);
        }

        private async Task OnWebSocketOrderReceivedAsync(object sender, 
            GdaxWssOrderReceived order)
        {
            await CallExecutedTradeHandlers(new ExecutedTrade(new Instrument(Name, order.ProductId),
                order.Time, order.Price ?? 0, order.Size,
                order.Side == GdaxOrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                order.OrderId.ToString(), ExecutionStatus.New));
        }

        private async Task OnOrderChangedAsync(object sender, GdaxWssOrderChange order)
        {
            await CallExecutedTradeHandlers(new ExecutedTrade(new Instrument(Name, order.ProductId),
                order.Time, order.Price ?? 0, order.NewSize,
                order.Side == GdaxOrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                order.OrderId.ToString(),
                ExecutionStatus.PartialFill));
        }

        private async Task OnWebSocketOrderDoneAsync(object sender, GdaxWssOrderDone order)
        {
            await CallExecutedTradeHandlers(new ExecutedTrade(new Instrument(Name, order.ProductId),
                order.Time, order.Price ?? 0, order.RemainingSize,
                order.Side == GdaxOrderSide.Buy ? TradeType.Buy : TradeType.Sell,
                order.OrderId.ToString(), 
                order.Reason == "cancelled" ? ExecutionStatus.Cancelled : ExecutionStatus.Fill));
        }

        private async Task LogAsync(string message, [CallerMemberName]string context = null)
        {
            const int maxMessageLength = 32000;

            if (LykkeLog == null)
                return;

            if (message.Length >= maxMessageLength)
                message = message.Substring(0, maxMessageLength);

            await LykkeLog.WriteInfoAsync(_gdaxExchangeTypeName, _gdaxExchangeTypeName, context, message);
        }

        private async Task LogAsync(Exception ex, [CallerMemberName]string context = null)
        {
            if (LykkeLog == null)
                return;

            await LykkeLog.WriteErrorAsync(_gdaxExchangeTypeName, _gdaxExchangeTypeName, context, ex);
        }
    }
}
