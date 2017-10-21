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
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal sealed class GdaxExchange : Exchange
    {
        public new static readonly string Name = "GDAX";
        private static readonly string _gdaxExchangeTypeName = nameof(GdaxExchange);

        private readonly GdaxExchangeConfiguration _configuration;
        private readonly IGdaxRestApi _exchangeApi;
        private readonly GdaxConverters _converters;

        public GdaxExchange(GdaxExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, ILog log) 
            : base(Name, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            _converters = new GdaxConverters(configuration, Name);

            var credenitals = new GdaxServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret, 
                _configuration.PassPhrase);
            _exchangeApi = new GdaxRestApi(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl),
                ConnectorUserAgent = configuration.UserAgent
            };
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, 
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = _converters.LykkeSymbolToGdaxSymbol(instrument.Name);
            var orderType = _converters.OrderTypeToGdaxOrderType(signal.OrderType);
            var side = _converters.TradeTypeToGdaxOrderSide(signal.TradeType);
            var volume = signal.Volume;
            var price = (!signal.Price.HasValue || signal.Price == 0 ) ? 1 : signal.Price.Value;
            var cts = CreateCancellationTokenSource(timeout);

            try
            {
                var response = await _exchangeApi.AddOrder(symbol, volume, price, side, orderType, cts.Token, 
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
                throw new ApiException("GDAX order id can be only Guid");

            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var response = await _exchangeApi.CancelOrder(id, cts.Token,
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
                throw new ApiException("GDAX order id can be only Guid");

            var cts = CreateCancellationTokenSource(timeout);
            try
            {
                var response = await _exchangeApi.GetOrderStatus(orderId, cts.Token);
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
                var response = await _exchangeApi.GetOpenOrders(cts.Token);
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
                var gdaxBalances = await _exchangeApi.GetBalances(cts.Token);
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
            throw new NotSupportedException();  // TODO
        }

        public override Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout)
        {
            throw new NotSupportedException(); // TODO
        }

        protected override void StartImpl()
        {
            OnConnected();
        }

        protected override void StopImpl()
        {

        }

        private static CancellationTokenSource CreateCancellationTokenSource(TimeSpan timeout)
        {
            return timeout == TimeSpan.Zero
                ? new CancellationTokenSource()
                : new CancellationTokenSource(timeout);
        }

        private void OnSentHttpRequest(object sender, SentHttpRequest request, 
            TranslatedSignalTableEntity translatedSignal)
        {
            var url = request.Uri.ToString();
            translatedSignal?.RequestSent(request.HttpMethod, url, request.Content);
            Log($"Making request to url: {url}. {translatedSignal?.RequestSentToExchange}");
        }

        private void OnReceivedHttpRequest(object sender, ReceivedHttpResponse response, 
            TranslatedSignalTableEntity translatedSignal)
        {
            translatedSignal?.ResponseReceived(response.Content);
        }

        private void Log(string message, [CallerMemberName]string context = null)
        {
            const int maxMessageLength = 32000;

            if (LykkeLog == null)
                return;

            if (message.Length >= maxMessageLength)
                message = message.Substring(0, maxMessageLength);

            LykkeLog.WriteInfoAsync(_gdaxExchangeTypeName, _gdaxExchangeTypeName, context, message);
        }
    }
}
