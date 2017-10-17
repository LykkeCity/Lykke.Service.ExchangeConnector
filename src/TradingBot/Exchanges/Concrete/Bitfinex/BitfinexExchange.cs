﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Bitfinex.RestClient;
using TradingBot.Exchanges.Concrete.Bitfinex.RestClient.Model;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Repositories;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Order = TradingBot.Exchanges.Concrete.Bitfinex.RestClient.Model.Order;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexExchange : Exchange
    {
        private readonly BitfinexExchangeConfiguration _configuration;
        private readonly IBitfinexApi _exchangeApi;
        public const string Bitfinex = "bitfinex";

        public BitfinexExchange(BitfinexExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, ILog log) : base(Bitfinex, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            var credenitals = new BitfinexServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret);
            _exchangeApi = new BitfinexApi(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl)
            };
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = ConvertSymbolFromLykkeToExchange(instrument.Name);
            var volume = signal.Volume;
            var orderType = ConvertOrderType(signal.OrderType);
            var side = ConvertTradeType(signal.TradeType);
            var price = signal.Price == 0 ? 1 : signal.Price ?? 1;
            var cts = new CancellationTokenSource(timeout);


            var response = await _exchangeApi.AddOrder(symbol, volume, price, side, orderType, cts.Token);

            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }

            var trade = OrderToTrade((Order)response);
            return trade;
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var cts = new CancellationTokenSource(timeout);
            if (!long.TryParse(signal.OrderId, out var id))
            {
                throw new ApiException("Bitfinex order id can be only integer");
            }
            var response = await _exchangeApi.CancelOrder(id, cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var trade = OrderToTrade((Order)response);
            return trade;
        }

        public override async Task<ExecutedTrade> GetOrder(string id, Instrument instrument)
        {
            if (!long.TryParse(id, out var orderId))
            {
                throw new ApiException("Bitfinex order id can be only integer");
            }
            var response = await _exchangeApi.GetOrderStatus(orderId);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var trade = OrderToTrade((Order)response);
            return trade;
        }

        public override async Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {

            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.GetActiveOrders(cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var trades = ((IReadOnlyCollection<Order>)response).Select(OrderToTrade);
            return trades;
        }

        private ExecutedTrade OrderToTrade(Order order)
        {
            var id = order.Id;
            var execTime = order.Timestamp;
            var execPrice = order.Price;
            var execVolume = order.ExecutedAmount;
            var tradeType = ConvertTradeType(order.Side);
            var status = ConvertExecutionStatus(order);
            var instr = ConvertSymbolFromExchangeToLykke(order.Symbol);

            return new ExecutedTrade(instr, execTime, execPrice, execVolume, tradeType, id, status);
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

        private string ConvertSymbolFromLykkeToExchange(string symbol)
        {
            if (!_configuration.CurrencyMapping.TryGetValue(symbol, out var result))
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to Bitfinex value");
            }
            return result;
        }

        private Instrument ConvertSymbolFromExchangeToLykke(string symbol)
        {
            var result = _configuration.CurrencyMapping.FirstOrDefault(kv => kv.Value == symbol).Key;
            if (result == null)
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to lykke value");
            }
            return new Instrument(Bitfinex, result);
        }


        private string ConvertOrderType(OrderType type)
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

        private static string ConvertTradeType(TradeType signalTradeType)
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

        private static TradeType ConvertTradeType(string signalTradeType)
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

        private static ExecutionStatus ConvertExecutionStatus(Order order)
        {
            if (order.IsCancelled)
            {
                return ExecutionStatus.Cancelled;
            }
            if (order.IsLive)
            {
                return ExecutionStatus.New;
            }
            return ExecutionStatus.Fill;
        }

    }
}