using System;
using System.Linq;
using QuickFix.Fields;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using ExecType = TradingBot.Trading.ExecType;
using TimeInForce = TradingBot.Trading.TimeInForce;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.Jfd
{
    internal sealed class JfdModelConverter : ExchangeConverters
    {
        private readonly IExchangeConfiguration _configuration;

        public JfdModelConverter(JfdExchangeConfiguration configuration) : base(configuration.SupportedCurrencySymbols, JfdExchange.Name)
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
            var jfdNorm = jfdSymbol.Replace("/", string.Empty);
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
                Message = report.IsSetText() ? report.Text.Obj : string.Empty
            };
            return executedTrade;
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
