using QuickFix.Fields;
using QuickFix.FIX44;
using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using ExecType = TradingBot.Trading.ExecType;
using ExecutionReport = TradingBot.Trading.ExecutionReport;
using TimeInForce = TradingBot.Trading.TimeInForce;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.Jfd
{
    internal sealed class JfdModelConverter : ExchangeConverters
    {
        private readonly JfdExchangeConfiguration _configuration;

        private static class OneZeroCustomTag
        {
            public const int OzAccountCurrency = 8880;
            public const int OzAccountBalance = 8881;
            public const int OzMarginUtilizationPercentage = 8882;
            public const int OzUsedMargin = 8883;
            public const int OzFreeMargin = 8884;
            public const int OzUnrealizedProfitOrLoss = 8885;
            public const int OzEquity = 8886;
        }

        public JfdModelConverter(JfdExchangeConfiguration configuration) : base(configuration.SupportedCurrencySymbols, JfdExchange.Name, configuration.UseSupportedCurrencySymbolsAsFilter)
        {
            _configuration = configuration;
        }

        public Side ConvertSide(TradeType tradeType)
        {
            return new Side(tradeType == TradeType.Buy ? Side.BUY : Side.SELL);
        }

        public TradeType ConvertSide(Side tradeType)
        {
            return Side.BUY == tradeType.Obj ? TradeType.Buy : TradeType.Sell;
        }

        public Symbol ConvertLykkeSymbol(string lykkeSymbol)
        {
            var result = _configuration.SupportedCurrencySymbols.FirstOrDefault(symb => symb.LykkeSymbol == lykkeSymbol);
            if (result == null)
            {
                throw new ArgumentException($"Symbol {lykkeSymbol} is not mapped to lykke value");
            }
            return new Symbol(result.ExchangeSymbol);
        }

        public Instrument ConvertJfdSymbol(Symbol jfdSymbol)
        {
            return ConvertJfdSymbol(jfdSymbol.Obj);
        }

        private Instrument ConvertJfdSymbol(string jfdSymbol)
        {
            var jfdNorm = jfdSymbol.Replace("/", String.Empty);
            var result = _configuration.SupportedCurrencySymbols.FirstOrDefault(symb => symb.ExchangeSymbol == jfdNorm);
            if (result == null)
            {
                throw new ArgumentException($"Symbol {jfdSymbol} is not mapped to lykke value");
            }
            return new Instrument(JfdExchange.Name, result.LykkeSymbol);
        }

        public ExecutionReport ConvertExecutionReport(QuickFix.FIX44.ExecutionReport report)
        {
            var executedTrade = new ExecutionReport
            {
                Instrument = ConvertJfdSymbol(report.Symbol),
                Time = report.TransactTime.Obj,
                Volume = report.CumQty.Obj,
                Type = ConvertSide(report.Side),
                ExchangeOrderId = report.OrderID.Obj,
                ExecutionStatus = ConvertStatus(report.OrdStatus),
                ClientOrderId = report.ClOrdID.Obj,
                ExecType = ConvertExecType(report.ExecType),
                OrderType = ConverOrderType(report.OrdType),
                Price = report.AvgPx.Obj,
                FailureType = OrderStatusUpdateFailureType.None,
                Success = !new[] { OrderExecutionStatus.Cancelled, OrderExecutionStatus.Rejected }.Contains(ConvertStatus(report.OrdStatus)),
                Message = report.IsSetText() ? report.Text.Obj : String.Empty
            };
            return executedTrade;
        }

        public IReadOnlyCollection<TradeBalanceModel> ConvertCollateral(IEnumerable<CollateralReport> collateralReports)
        {

            var models = new List<TradeBalanceModel>();
            foreach (var report in collateralReports)
            {
                var instr = report.IsSetField((int)OneZeroCustomTag.OzAccountCurrency) ? report.GetString(OneZeroCustomTag.OzAccountCurrency) : "NoSymbol";
                var model = new TradeBalanceModel
                {
                    AccountCurrency = instr,
                    Totalbalance = report.IsSetField((int)OneZeroCustomTag.OzEquity) ? report.GetDecimal(OneZeroCustomTag.OzEquity) : -1,
                    UnrealisedPnL = report.IsSetField((int)OneZeroCustomTag.OzUnrealizedProfitOrLoss) ? report.GetDecimal(OneZeroCustomTag.OzUnrealizedProfitOrLoss) : -1,
                    MaringAvailable = report.IsSetField((int)OneZeroCustomTag.OzFreeMargin) ? report.GetDecimal(OneZeroCustomTag.OzFreeMargin) : -1,
                    MarginUsed = report.IsSetField((int)OneZeroCustomTag.OzUsedMargin) ? report.GetDecimal(OneZeroCustomTag.OzUsedMargin) : -1
                };
                models.Add(model);
            }

            return models;
        }

        public IReadOnlyCollection<PositionModel> ConvertPositionReport(IReadOnlyCollection<PositionReport> reports)
        {
            var result = new List<PositionModel>(reports.Count);
            foreach (var report in reports)
            {
                var inst = ConvertJfdSymbol(report.Symbol);
                var details = report.GetGroup(1, new PositionReport.NoPositionsGroup());
                var longQty = details.GetField(new LongQty()).Obj;
                var shortQty = details.GetField(new ShortQty()).Obj;
                var usedMagin = details.IsSetField((int)OneZeroCustomTag.OzUsedMargin) ? details.GetDecimal(OneZeroCustomTag.OzUsedMargin) : 0m;
                var unrealizedPnl = details.IsSetField((int)OneZeroCustomTag.OzUnrealizedProfitOrLoss) ? details.GetDecimal(OneZeroCustomTag.OzUnrealizedProfitOrLoss) : 0m;

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


        public OrdType ConverOrderType(OrderType orderType)
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


        public OrderType ConverOrderType(OrdType orderType)
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

        public QuickFix.Fields.TimeInForce ConvertTimeInForce(TimeInForce timeInForce)
        {
            switch (timeInForce)
            {
                case TimeInForce.GoodTillCancel:
                    return new QuickFix.Fields.TimeInForce(QuickFix.Fields.TimeInForce.GOOD_TILL_CANCEL);
                case TimeInForce.FillOrKill:
                    return new QuickFix.Fields.TimeInForce(QuickFix.Fields.TimeInForce.FILL_OR_KILL);
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeInForce), timeInForce, null);
            }
        }
    }
}
