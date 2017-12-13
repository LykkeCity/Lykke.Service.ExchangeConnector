using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;
using QuickFix.Fields;
using TradingBot.Exchanges.Concrete.AutorestClient.Models;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;
using Position = TradingBot.Exchanges.Concrete.AutorestClient.Models.Position;
using OrdStatus = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.OrdStatus;
using Side = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Side;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexModelConverter: ExchangeConverters
    {
        private const decimal SatoshiRate = 100000000;

        public BitMexModelConverter(IReadOnlyCollection<CurrencySymbol> currencySymbols,
            string exchangeName): base(currencySymbols, exchangeName)
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

        public static ExecutedTrade OrderToTrade(Order order)
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


        public ExecutedTrade OrderToTrade(WebSocketClient.Model.RowItem row)
        {
            var lykkeInstrument = this.ExchangeSymbolToLykkeInstrument(row.Symbol);
            return new ExecutedTrade(
                lykkeInstrument,
                row.Timestamp,
                row.Price ?? row.AvgPx ?? 0,
                row.OrderQty ?? row.CumQty ?? 0,
                row.Side.HasValue ? ConvertSideToModel(row.Side.Value) : TradeType.Unknown,
                row.OrderID,
                row.OrdStatus.HasValue ? ConvertExecutionStatusToModel(row.OrdStatus.Value) : ExecutionStatus.Unknown);
        }

        public Acknowledgement OrderToAck(WebSocketClient.Model.RowItem row)
        {
            var lykkeInstrument = this.ExchangeSymbolToLykkeInstrument(row.Symbol);
            return new Acknowledgement()
            {
                Instrument = lykkeInstrument.Name,
                Exchange = lykkeInstrument.Exchange,
                ClientOrderId = row.ClOrdID,
                ExchangeOrderId = row.OrderID,
                Success = true
            };
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

        public static TradeType ConvertTradeType(string signalTradeType)
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
                    return TradeType.Buy;
                case Side.Sell:
                    return TradeType.Sell;
                default:
                    return TradeType.Unknown;
            }
        }

        public static ExecutionStatus ConvertExecutionStatusToModel(OrdStatus status)
        {
            switch (status)
            {
                case OrdStatus.New:
                    return ExecutionStatus.New;
                case OrdStatus.PartiallyFilled:
                    return ExecutionStatus.PartialFill;
                case OrdStatus.Filled:
                    return ExecutionStatus.Fill;
                case OrdStatus.Canceled:
                    return ExecutionStatus.Cancelled;
                default:
                    return ExecutionStatus.Unknown;
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
                case "Rejeсted":
                    return ExecutionStatus.Rejected;
                default:
                    return ExecutionStatus.Unknown;
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

        public TickPrice QuoteToModel(WebSocketClient.Model.RowItem row)
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
