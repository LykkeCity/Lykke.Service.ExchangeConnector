using System;
using System.Collections.Generic;
using System.Linq;
using QuickFix.Fields;
using QuickFix.FIX44;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using ExecType = TradingBot.Trading.ExecType;
using ExecutionReport = TradingBot.Trading.ExecutionReport;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;
using TimeInForce = QuickFix.Fields.TimeInForce;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal sealed class IcmModelConverter : ExchangeConverters
    {
        private readonly IcmExchangeConfiguration _configuration;

        public IcmModelConverter(IcmExchangeConfiguration configuration) : base(configuration.SupportedCurrencySymbols, IcmExchange.Name)
        {
            _configuration = configuration;
        }

        public TickPrice ToTickPrice(OrderBook orderBook)
        {
            if (orderBook.Asks != null && orderBook.Asks.Any() && orderBook.Bids != null && orderBook.Bids.Any())
            {
                return new TickPrice(new Instrument(IcmExchange.Name, orderBook.Asset),
                    orderBook.Timestamp,
                    orderBook.Asks.Select(x => x.Price).Min(),
                    orderBook.Bids.Select(x => x.Price).Max()
                );
            }

            return null;
        }

        public NewOrderSingle CreateNewOrderSingle(TradingSignal tradingSignal)
        {
            var newOrderSingle = new NewOrderSingle
            {
                Symbol = new Symbol(LykkeSymbolToExchangeSymbol(tradingSignal.Instrument.Name)),
                Currency = new Currency(tradingSignal.Instrument.Base),
                Side = ConvertSide(tradingSignal.TradeType),
                OrderQty = new OrderQty(tradingSignal.Volume),
                OrdType = ConvertOrderType(tradingSignal.OrderType),
                TimeInForce = new TimeInForce(TimeInForce.IMMEDIATE_OR_CANCEL),
                TransactTime = new TransactTime(DateTime.UtcNow),
                Price = new Price(tradingSignal.Price ?? 0m)
            };

            return newOrderSingle;
        }

        public ExecutionReport ConvertExecutionReport(QuickFix.FIX44.ExecutionReport report)
        {
            var executedTrade = new ExecutionReport
            {
                Instrument = ExchangeSymbolToLykkeInstrument(report.Symbol.Obj),
                Time = report.IsSetTransactTime() ? report.TransactTime.Obj : DateTime.UtcNow,
                Volume = report.CumQty.Obj,
                Type = ConvertSide(report.Side),
                ExchangeOrderId = report.OrderID.Obj,
                ExecutionStatus = ConvertStatus(report.OrdStatus),
                ClientOrderId = report.ClOrdID.Obj,
                ExecType = ConvertExecType(report.ExecType),
                OrderType = report.IsSetOrdType() ? ConvertOrderType(report.OrdType) : OrderType.Unknown,
                Price = report.IsSetAvgPx() ? report.AvgPx.Obj : 0m,
                FailureType = OrderStatusUpdateFailureType.None,
                Success = !new[] { OrderExecutionStatus.Cancelled, OrderExecutionStatus.Rejected }.Contains(ConvertStatus(report.OrdStatus)),
                Message = report.IsSetText() ? report.Text.Obj : string.Empty
            };
            return executedTrade;
        }

        public IReadOnlyCollection<PositionModel> ConvertPositionReport(IReadOnlyCollection<PositionReport> reports)
        {
            var result = new List<PositionModel>(reports.Count);
            foreach (var report in reports)
            {
                var inst = ExchangeSymbolToLykkeInstrument(report.Symbol.Obj);
                var details = report.GetGroup(1, new PositionReport.NoPositionsGroup());
                var longQty = details.IsSetField(new LongQty()) ? details.GetField(new LongQty()).Obj : 0m;
                var shortQty = details.IsSetField(new ShortQty()) ? details.GetField(new ShortQty()).Obj : 0m;
                var usedMagin = 0m;
                var unrealizedPnl = 0m;

                var setting = _configuration.SupportedCurrencySymbols.FirstOrDefault(i => string.Equals(i.ExchangeSymbol, inst.Name, StringComparison.InvariantCultureIgnoreCase));
                var initialMargin = setting?.InitialMarginPercent ?? -1;
                var maintMargin = setting?.MaintMarginPercent ?? -1;

                var model = new PositionModel
                {
                    Symbol = inst.Name,
                    PositionVolume = longQty - shortQty,
                    MaintMarginUsed = usedMagin,
                    RealisedPnL = 0,
                    UnrealisedPnL = unrealizedPnl,
                    PositionValue = null,
                    AvailableMargin = null,
                    InitialMarginRequirement = initialMargin,
                    MaintenanceMarginRequirement = maintMargin,
                };
                result.Add(model);
            }
            return result;
        }

        private static TradeType ConvertSide(Side tradeType)
        {
            return Side.BUY == tradeType.Obj ? TradeType.Buy : TradeType.Sell;
        }


        private OrderType ConvertOrderType(OrdType orderType)
        {
            switch (orderType.Obj)
            {
                case OrdType.MARKET:
                    return OrderType.Market;
                case OrdType.LIMIT:
                    return OrderType.Limit;
                default:
                    return OrderType.Unknown;
            }
        }


        private static ExecType ConvertExecType(QuickFix.Fields.ExecType reportExecType)
        {
            switch (reportExecType.Obj)
            {
                case QuickFix.Fields.ExecType.TRADE:
                    return ExecType.Trade;
                default:
                    return ExecType.Unknown;
            }
        }

        private static OrderExecutionStatus ConvertStatus(OrdStatus status)
        {
            switch (status.Obj)
            {
                case OrdStatus.PARTIALLY_FILLED:
                    return OrderExecutionStatus.PartialFill;
                case OrdStatus.FILLED:
                    return OrderExecutionStatus.Fill;
                case OrdStatus.NEW:
                    return OrderExecutionStatus.New;
                case OrdStatus.CANCELED:
                    return OrderExecutionStatus.Cancelled;
                case OrdStatus.REJECTED:
                    return OrderExecutionStatus.Rejected;
                default:
                    return OrderExecutionStatus.Unknown;
            }
        }

        private OrdType ConvertOrderType(OrderType orderType)
        {
            char typeName;

            switch (orderType)
            {
                case OrderType.Market:
                    typeName = OrdType.MARKET;
                    break;
                case OrderType.Limit:
                    typeName = OrdType.LIMIT;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }

            return new OrdType(typeName);
        }

        private static Side ConvertSide(TradeType tradeType)
        {
            return new Side(tradeType == TradeType.Buy ? Side.BUY : Side.SELL);
        }
    }
}
