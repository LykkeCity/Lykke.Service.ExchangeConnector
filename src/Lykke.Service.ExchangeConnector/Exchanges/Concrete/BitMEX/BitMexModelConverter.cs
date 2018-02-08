using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Order = Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models.Order;
using Position = Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models.Position;
using OrdStatus = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.OrdStatus;
using Side = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.Side;
using RowItem = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.RowItem;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexModelConverter : ExchangeConverters
    {
        private const decimal SatoshiRate = 100000000;

        public BitMexModelConverter(IReadOnlyCollection<CurrencySymbol> currencySymbols) : base(currencySymbols, BitMexExchange.Name)
        {

        }

        public static PositionModel ExchangePositionToModel(Position position)
        {
            return new PositionModel
            {
                // Symbol = ConvertSymbolFromBitMexToLykke(position.Symbol, configuration).Name,
                Symbol = "USDBTC", //HACK Hard code!
                PositionVolume = -Convert.ToDecimal(position.CurrentQty),
                MaintMarginUsed = Convert.ToDecimal(position.MaintMargin) / SatoshiRate,
                RealisedPnL = Convert.ToDecimal(position.RealisedPnl) / SatoshiRate,
                UnrealisedPnL = Convert.ToDecimal(position.UnrealisedPnl) / SatoshiRate,
                PositionValue = Convert.ToDecimal(position.MarkValue) / SatoshiRate,
                AvailableMargin = 0, // Nothing to map
                InitialMarginRequirement = Convert.ToDecimal(position.InitMarginReq),
                MaintenanceMarginRequirement = Convert.ToDecimal(position.MaintMarginReq)
            };
        }

        private static Instrument GetInstrument()
        {
            var instr = new Instrument(BitMexExchange.Name, "USDBTC"); //HACK Hard code!
            return instr;
        }

        public static ExecutionReport OrderToTrade(Order order)
        {
            var execTime = order.TransactTime ?? DateTime.UtcNow;
            var execPrice = (decimal)(order.AvgPx ?? 0);
            var execVolume = (decimal)(order.CumQty ?? 0);
            var tradeType = ConvertTradeType(order.Side);
            var status = ConvertExecutionStatus(order.OrdStatus);
            var instr = GetInstrument();
            return new ExecutionReport(instr, execTime, execPrice, execVolume, tradeType, order.OrderID, status)
            {
                ClientOrderId = order.ClOrdID,
                Message = order.Text,
                Success = true,
                OrderType = ConvertOrderType(order.OrdType),
                ExecType = ExecType.Trade,
                FailureType = OrderStatusUpdateFailureType.None
            };
        }


        public static ExecutionReport OrderToTrade(RowItem row)
        {
            var lykkeInstrument = GetInstrument();
            return new ExecutionReport(
                lykkeInstrument,
                row.Timestamp,
                row.Price ?? row.AvgPx ?? 0,
                row.OrderQty ?? row.CumQty ?? 0,
                row.Side.HasValue ? ConvertSideToModel(row.Side.Value) : TradeType.Unknown,
                row.OrderID,
               ConvertExecutionStatusToModel(row.OrdStatus))
            {
                ClientOrderId = row.ClOrdID,
                Message = row.Text,
                Success = true,
                OrderType = ConvertOrderType(row.OrdType),
                ExecType = ConvertExecType(row.ExecType)
            };
        }

        private static ExecType ConvertExecType(string rowExecType)
        {
            if (Enum.TryParse(typeof(ExecType), rowExecType, out var type))
            {
                return (ExecType)type;
            }

            return ExecType.Unknown;
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

        public static OrderType ConvertOrderType(string type)
        {
            switch (type)
            {
                case "Market":
                    return OrderType.Market;
                case "Limit":
                    return OrderType.Limit;
                default:
                    return OrderType.Unknown;
            }
        }

        public static double ConvertVolume(decimal volume)
        {
            return (double)volume;
        }

        public static string ConvertTradeType(TradeType signalTradeType)
        {
            // HACK!!! the direction is inverted
            switch (signalTradeType)
            {
                case TradeType.Sell:
                    return "Buy";
                case TradeType.Buy:
                    return "Sell";
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        private static TradeType ConvertTradeType(string signalTradeType)
        {
            // HACK!!! the direction is inverted
            switch (signalTradeType)
            {
                case "Buy":
                    return TradeType.Sell;
                case "Sell":
                    return TradeType.Buy;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        public static TradeType ConvertSideToModel(Side side)
        {
            switch (side)
            {
                case Side.Buy:
                    return TradeType.Sell;
                case Side.Sell:
                    return TradeType.Buy;
                default:
                    return TradeType.Unknown;
            }
        }

        public static OrderExecutionStatus ConvertExecutionStatusToModel(OrdStatus? status)
        {
            switch (status)
            {
                case OrdStatus.New:
                    return OrderExecutionStatus.New;
                case OrdStatus.PartiallyFilled:
                    return OrderExecutionStatus.PartialFill;
                case OrdStatus.Filled:
                    return OrderExecutionStatus.Fill;
                case OrdStatus.Canceled:
                    return OrderExecutionStatus.Cancelled;
                default:
                    return OrderExecutionStatus.Unknown;
            }
        }

        public static OrderExecutionStatus ConvertExecutionStatus(string executionStatus)
        {
            switch (executionStatus)
            {
                case "New":
                    return OrderExecutionStatus.New;
                case "Filled":
                    return OrderExecutionStatus.Fill;
                case "Partially Filled":
                    return OrderExecutionStatus.PartialFill;
                case "Canceled":
                    return OrderExecutionStatus.Cancelled;
                case "Rejeсted":
                    return OrderExecutionStatus.Rejected;
                default:
                    return OrderExecutionStatus.Unknown;
            }
        }


        public static OrderBookItem ConvertBookItem(RowItem row)
        {
            return new OrderBookItem
            {
                Id = row.Id.ToString(CultureInfo.InvariantCulture),
                IsBuy = row.Side == Side.Buy,
                Price = row.Price ?? 0,
                Symbol = row.Symbol,
                Size = row.Size
            };
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

        public TickPrice QuoteToModel(RowItem row)
        {
            if (row.AskPrice.HasValue && row.BidPrice.HasValue)
            {
                var lykkeInstrument = this.ExchangeSymbolToLykkeInstrument(row.Symbol);
                return new TickPrice(lykkeInstrument, row.Timestamp, row.AskPrice.Value, row.BidPrice.Value);
            }
            else
            {
                throw new ArgumentException($"Ask/bid price is not specified for a quote. Message: '{JsonConvert.SerializeObject(row)}'", nameof(row));
            }
        }



    }
}
