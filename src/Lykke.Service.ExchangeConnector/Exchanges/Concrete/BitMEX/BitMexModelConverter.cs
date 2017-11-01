﻿using System;
using System.Linq;
using TradingBot.Exchanges.Concrete.AutorestClient.Models;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;
using Position = TradingBot.Exchanges.Concrete.AutorestClient.Models.Position;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal static class BitMexModelConverter
    {
        private const decimal SatoshiRate = 100000000;

        public static PositionModel ExchangePositionToModel(Position position, ICurrencyMappingProvider configuration)
        {
            return new PositionModel
            {
                // Symbol = ConvertSymbolFromBitMexToLykke(position.Symbol, configuration).Name,
                Symbol = "USDBTC", //HACK Hard code!
                PositionVolume = Convert.ToDecimal(position.CurrentQty),
                MaintMarginUsed = Convert.ToDecimal(position.MaintMargin) / SatoshiRate,
                RealisedPnL = Convert.ToDecimal(position.RealisedPnl) / SatoshiRate,
                UnrealisedPnL = Convert.ToDecimal(position.UnrealisedPnl) / SatoshiRate,
                PositionValue = -Convert.ToDecimal(position.MarkValue) / SatoshiRate,
                AvailableMargin = 0, // Nothing to map
                InitialMarginRequirement = Convert.ToDecimal(position.InitMarginReq),
                MaintenanceMarginRequirement = Convert.ToDecimal(position.MaintMarginReq)
            };
        }

        public static ExecutedTrade OrderToTrade(Order order, ICurrencyMappingProvider configuration)
        {

            var execTime = order.TransactTime ?? DateTime.UtcNow;
            var execPrice = (decimal)(order.Price ?? 0);
            var execVolume = (decimal)(order.OrderQty ?? 0);
            var tradeType = ConvertTradeType(order.Side);
            var status = ConvertExecutionStatus(order.OrdStatus);
            //  var instr = ConvertSymbolFromBitMexToLykke(order.Symbol, configuration);
            var instr = new Instrument(BitMexExchange.Name, "USDBTC"); //HACK Hard code!

            return new ExecutedTrade(instr, execTime, execPrice, execVolume, tradeType, order.OrderID, status) { Message = order.Text };
        }

        public static string ConvertSymbolFromLykkeToBitMex(string symbol, ICurrencyMappingProvider configuration)
        {
            if (!configuration.CurrencyMapping.TryGetValue(symbol, out var result))
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to BitMex value");
            }
            return result;
        }

        public static Instrument ConvertSymbolFromBitMexToLykke(string symbol, ICurrencyMappingProvider configuration)
        {
            var result = configuration.CurrencyMapping.FirstOrDefault(kv => kv.Value == symbol).Key;
            if (result == null)
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to lykke value");
            }
            return new Instrument(BitMexExchange.Name, result);
        }

        public static string ConvertOrderType(OrderType type)
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

        public static double ConvertVolume(decimal volume)
        {
            return (double)volume;
        }

        public static string ConvertTradeType(TradeType signalTradeType)
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

        public static TradeType ConvertTradeType(string signalTradeType)
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

        public static ExecutionStatus ConvertExecutionStatus(string executionStatus)
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

        public static TradeBalanceModel ExchangeBalanceToModel(Margin bitmexMargin)
        {
            var model = new TradeBalanceModel
            {
                AccountCurrency = "BTC", // The only currency supported
                Totalbalance = Convert.ToDecimal(bitmexMargin.MarginBalance) / SatoshiRate,
                UnrealisedPnL = Convert.ToDecimal(bitmexMargin.UnrealisedPnl) / SatoshiRate,
                MaringAvailable = Convert.ToDecimal(bitmexMargin.AvailableMargin) / SatoshiRate,
                MarginUsed = Convert.ToDecimal(bitmexMargin.MaintMargin) / SatoshiRate
            };
            return model;
        }
    }
}