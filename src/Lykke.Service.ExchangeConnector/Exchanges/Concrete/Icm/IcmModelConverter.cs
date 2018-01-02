using System;
using System.Linq;
using QuickFix.Fields;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using ExecType = TradingBot.Trading.ExecType;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal sealed class IcmModelConverter : ExchangeConverters
    {
        public IcmModelConverter(IcmConfig configuration) : base(configuration.SupportedCurrencySymbols, IcmExchange.Name)
        {

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

        public ExecutionReport ConvertExecutionReport(QuickFix.FIX44.ExecutionReport report, OrderExecutionStatus orderExecutionStatus)
        {
            var executedTrade = new ExecutionReport
            {
                Instrument = ExchangeSymbolToLykkeInstrument(report.Symbol.Obj),
                Time = report.TransactTime.Obj,
                Price = orderExecutionStatus == OrderExecutionStatus.Fill || orderExecutionStatus == OrderExecutionStatus.PartialFill ? report.AvgPx.Obj : report.Price.Obj,
                Volume = report.OrderQty.Obj,
                Type = report.Side.Obj == Side.BUY ? TradeType.Buy : TradeType.Sell,
                ExchangeOrderId = report.OrderID.Obj,
                ExecutionStatus = orderExecutionStatus,
                ClientOrderId = report.ClOrdID.Obj,
                OrderType = CovertOrderType(report.OrdType.Obj),
                ExecType = ExecType.Unknown,
                Message = report.IsSetText() ? report.Text.Obj : string.Empty
            };

            if (report.IsSetField(Tags.Text))
            {
                executedTrade.Message = report.Text.Obj;
            }

            return executedTrade;
        }

        private static OrderType CovertOrderType(char ordTypeObj)
        {
            switch (ordTypeObj)
            {
                case OrdType.LIMIT:
                    return OrderType.Limit;
                case OrdType.MARKET:
                    return OrderType.Market;
                default:
                    return OrderType.Unknown;
            }
        }
    }
}
