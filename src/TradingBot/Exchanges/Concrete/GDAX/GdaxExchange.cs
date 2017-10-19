using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Repositories;
using TradingBot.Trading;
using GdaxOrder = TradingBot.Exchanges.Concrete.GDAX.RestClient.Model.GdaxOrder;
using Instrument = TradingBot.Trading.Instrument;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal sealed class GdaxExchange : Exchange
    {
        private readonly GdaxExchangeConfiguration _configuration;
        private readonly IGdaxApi _exchangeApi;
        public const string GDAX = "GDAX";

        public GdaxExchange(GdaxExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, ILog log) 
            : base(GDAX, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            var credenitals = new GdaxServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret, 
                _configuration.PassPhrase);
            _exchangeApi = new GdaxApi(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl),
                ConnectorUserAgent = configuration.UserAgent
            };
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, 
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = LykkeSymbolToGdaxSymbol(instrument.Name);
            var volume = signal.Volume;
            var orderType = OrderTypeToGdaxOrderType(signal.OrderType);
            var side = TradeTypeToGdaxTradeType(signal.TradeType);
            var price = signal.Price == 0 ? 1 : signal.Price ?? 1;
            var cts = new CancellationTokenSource(timeout);

            try
            {
                var response = await _exchangeApi.AddOrder(symbol, volume, price, side, orderType, cts.Token);
                var trade = OrderToTrade(response);
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

            var cts = new CancellationTokenSource(timeout);
            try
            {
                var response = await _exchangeApi.CancelOrder(id, cts.Token);
                var trade = OrderToTrade(response);
                return trade;
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

            var cts = new CancellationTokenSource(timeout);
            try
            {
                var response = await _exchangeApi.GetOrderStatus(orderId, cts.Token);
                var trade = OrderToTrade(response);
                return trade;
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        public override async Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            try
            {
                var response = await _exchangeApi.GetOpenOrders(cts.Token);
                var trades = response.Select(OrderToTrade);
                return trades;
            }
            catch (StatusCodeException ex)
            {
                throw new ApiException(ex.Message);
            }
        }

        protected override Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal)
        {
            throw new NotImplementedException();
        }

        protected override void StartImpl()
        {
            OnConnected();
        }

        protected override void StopImpl()
        {

        }

        private ExecutedTrade OrderToTrade(GdaxOrder order)
        {
            var id = order.Id;
            var execTime = order.CreatedAt;
            var execPrice = order.Price;
            var execVolume = order.ExecutedValue;
            var tradeType = GdaxTradeTypeToTradeType(order.Side);
            var status = GdaxOrderStatusToExecutionStatus(order);
            var instr = LykkeSymbolToGdaxInstrument(order.ProductId);

            return new ExecutedTrade(instr, execTime, execPrice, execVolume, tradeType, id.ToString(), status);
        }

        private string LykkeSymbolToGdaxSymbol(string symbol)
        {
            if (!_configuration.CurrencyMapping.TryGetValue(symbol, out var result))
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to GDAX value");
            }
            return result;
        }

        private Instrument LykkeSymbolToGdaxInstrument(string symbol)
        {
            var result = _configuration.CurrencyMapping.FirstOrDefault(kv => kv.Value == symbol).Key;
            if (result == null)
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to lykke value");
            }
            return new Instrument(GDAX, result);
        }
        
        private static string OrderTypeToGdaxOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:
                    return "market";
                case OrderType.Limit:
                    return "limit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static string TradeTypeToGdaxTradeType(TradeType signalTradeType)
        {
            switch (signalTradeType)
            {
                case TradeType.Buy:
                    return "buy";
                case TradeType.Sell:
                    return "sell";
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        private static TradeType GdaxTradeTypeToTradeType(string signalTradeType)
        {
            switch (signalTradeType)
            {
                case "buy":
                    return TradeType.Buy;
                case "sell":
                    return TradeType.Sell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        private static ExecutionStatus GdaxOrderStatusToExecutionStatus(GdaxOrder order)
        {
            switch (order.Status)
            {
                case "open":
                    return ExecutionStatus.New;
                case "pending":
                    return ExecutionStatus.Pending;
                case "active":  // Is this correct - Investigate
                    return ExecutionStatus.PartialFill;
                case "cancelled":  // do we have such status? Investigate
                    return ExecutionStatus.Cancelled;
                case "done":
                    return ExecutionStatus.Fill;
            }

            return ExecutionStatus.Unknown;
        }
    }
}
