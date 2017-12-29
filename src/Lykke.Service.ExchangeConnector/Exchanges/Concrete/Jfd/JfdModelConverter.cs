using System;
using System.Linq;
using QuickFix.Fields;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TimeInForce = TradingBot.Trading.TimeInForce;
using TradeType = TradingBot.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.Jfd
{
    internal sealed class JfdModelConverter
    {
        private readonly IExchangeConfiguration _configuration;

        public JfdModelConverter(JfdExchangeConfiguration configuration)
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

        public Instrument ConvertJfdSymbol(string jfdSymbol)
        {
            var jfdNorm = jfdSymbol.Replace("/", string.Empty);
            var result = _configuration.SupportedCurrencySymbols.FirstOrDefault(symb => symb.ExchangeSymbol == jfdNorm);
            if (result == null)
            {
                throw new ArgumentException($"Symbol {jfdSymbol} is not mapped to lykke value");
            }
            return new Instrument(JfdExchange.Name, result.LykkeSymbol);
        }

        public OrderExecutionStatus ConvertStatus(OrdStatus status)
        {
            switch (status.Obj)
            {
                case OrdStatus.PARTIALLY_FILLED:
                    return OrderExecutionStatus.PartialFill;
                case OrdStatus.FILLED:
                    return OrderExecutionStatus.Fill;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status.Obj.ToString());
            }
        }


        public OrdType ConvertType(OrderType orderType)
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
