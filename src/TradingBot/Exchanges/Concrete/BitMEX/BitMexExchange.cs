using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.AutorestClient;
using TradingBot.Exchanges.Concrete.AutorestClient.Models;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Repositories;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexExchange : Exchange
    {
        private readonly BitMexExchangeConfiguration _configuration;
        private readonly IBitMEXAPI _exchangeApi;
        public const string BitMex = "bitmex";

        public BitMexExchange(BitMexExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, ILog log) : base(BitMex, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            var credenitals = new BitMexServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret);
            _exchangeApi = new BitMEXAPI(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl)
            };
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = ConvertSymbolFromLykkeToBitMex(instrument.Name);
            var volume = ConvertVolume(signal.Volume);
            var orderType = ConvertOrderType(signal.OrderType);
            var side = ConvertTradeType(signal.TradeType);
            var price = (double?)signal.Price;
            var ct = new CancellationTokenSource(timeout);


            var response = await _exchangeApi.OrdernewAsync(symbol, orderQty: volume, price: price, ordType: orderType, side: side, cancellationToken: ct.Token);

            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }

            var order = (Order)response;

            var execStatus = ConvertExecutionStatus(order.OrdStatus);
            var execPrice = (decimal)(order.Price ?? 0d);
            var execVolume = (decimal)(order.OrderQty ?? 0d);
            var exceTime = order.TransactTime ?? DateTime.UtcNow;
            var execType = ConvertTradeType(order.Side);

            return new ExecutedTrade(instrument, exceTime, execPrice, execVolume, execType, order.OrderID, execStatus) { Message = order.Text };
        }



        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var ct = new CancellationTokenSource(timeout);
            var id = signal.OrderId;
            var response = await _exchangeApi.OrdercancelAsync(cancellationToken: ct.Token, orderID: id);

            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }
            var res = EnsureCorrectResponse(id, response);
            return OrderToTrade(res[0]);
        }

        public override async Task<ExecutedTrade> GetOrder(string id, Instrument instrument)
        {
            var filterObj = new { orderID = id };
            var filterArg = JsonConvert.SerializeObject(filterObj);
            var response = await _exchangeApi.OrdergetOrdersAsync(filter: filterArg);
            var res = EnsureCorrectResponse(id, response);
            return OrderToTrade(res[0]);
        }

        public override async Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            var filterObj = new { ordStatus = "New" };
            var filterArg = JsonConvert.SerializeObject(filterObj);
            var response = await _exchangeApi.OrdergetOrdersAsync(filter: filterArg);
            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }

            var trades = ((IReadOnlyCollection<Order>)response).Select(OrderToTrade);
            return trades;
        }

        private ExecutedTrade OrderToTrade(Order order)
        {

            var execTime = order.TransactTime ?? DateTime.UtcNow;
            var execPrice = (decimal)(order.Price ?? 0);
            var execVolume = (decimal)(order.OrderQty ?? 0);
            var tradeType = ConvertTradeType(order.Side);
            var status = ConvertExecutionStatus(order.OrdStatus);
            var instr = ConvertSymbolFromBiMexToLykke(order.Symbol);

            return new ExecutedTrade(instr, execTime, execPrice, execVolume, tradeType, order.OrderID, status) { Message = order.Text };
        }

        private static IReadOnlyList<Order> EnsureCorrectResponse(string id, object response)
        {
            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }
            var res = (IReadOnlyList<Order>)response;
            if (res.Count != 1)
            {
                throw new InvalidOperationException($"Received {res.Count} orders. Expected exactly one with id {id}");
            }
            return res;
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

        private string ConvertSymbolFromLykkeToBitMex(string symbol)
        {
            if (!_configuration.CurrencyMapping.TryGetValue(symbol, out var result))
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to BitMex value");
            }
            return result;
        }

        private Instrument ConvertSymbolFromBiMexToLykke(string symbol)
        {
            var result = _configuration.CurrencyMapping.FirstOrDefault(kv => kv.Value == symbol).Key;
            if (result == null)
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to lykke value");
            }
            return new Instrument(BitMex, result);
        }


        private string ConvertOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:
                    return "Market";
                case OrderType.Limit:
                    return "Limit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static double ConvertVolume(decimal volume)
        {
            return (double)volume;
        }

        private static string ConvertTradeType(TradeType signalTradeType)
        {
            switch (signalTradeType)
            {
                case TradeType.Buy:
                    return "Buy";
                case TradeType.Sell:
                    return "Sell";
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        private static TradeType ConvertTradeType(string signalTradeType)
        {
            switch (signalTradeType)
            {
                case "Buy":
                    return TradeType.Buy;
                case "Sell":
                    return TradeType.Sell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        private ExecutionStatus ConvertExecutionStatus(string executionStatus)
        {
            switch (executionStatus)
            {
                case "New":
                    return ExecutionStatus.New;
                case "Filled":
                    return ExecutionStatus.Fill;
                case "Partially Filled":
                    return ExecutionStatus.PartialFill;
                case "Canceled":
                    return ExecutionStatus.Cancelled;
                default:
                    return ExecutionStatus.Unknown;
            }
        }

    }
}
