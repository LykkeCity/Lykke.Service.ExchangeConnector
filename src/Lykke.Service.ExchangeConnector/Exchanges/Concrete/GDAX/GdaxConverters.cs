using System;
using System.Linq;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal class GdaxConverters
    {
        private readonly GdaxExchangeConfiguration _configuration;
        private readonly string _exchangeName;

        public GdaxConverters(GdaxExchangeConfiguration configuration, string exchangeName)
        {
            _configuration = configuration;
            _exchangeName = exchangeName;
        }

        public ExecutedTrade OrderToTrade(GdaxOrderResponse order)
        {
            var id = order.Id;
            var execTime = order.CreatedAt;
            var execPrice = order.Price;
            var execVolume = order.ExecutedValue;
            var tradeType = GdaxOrderSideToTradeType(order.Side);
            var status = GdaxOrderStatusToExecutionStatus(order);
            var instr = LykkeSymbolToGdaxInstrument(order.ProductId);

            return new ExecutedTrade(instr, execTime, execPrice, execVolume,
                tradeType, id.ToString(), status);
        }

        public string LykkeSymbolToGdaxSymbol(string symbol)
        {
            if (!_configuration.CurrencyMapping.TryGetValue(symbol, out var result))
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to GDAX value");
            }
            return result;
        }

        public Instrument LykkeSymbolToGdaxInstrument(string symbol)
        {
            var result = _configuration.CurrencyMapping.FirstOrDefault(kv => kv.Value == symbol).Key;
            if (result == null)
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to lykke value");
            }
            return new Instrument(_exchangeName, result);
        }

        public GdaxOrderType OrderTypeToGdaxOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:
                    return GdaxOrderType.Market;
                case OrderType.Limit:
                    return GdaxOrderType.Limit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public GdaxOrderSide TradeTypeToGdaxOrderSide(TradeType signalTradeType)
        {
            switch (signalTradeType)
            {
                case TradeType.Buy:
                    return GdaxOrderSide.Buy;
                case TradeType.Sell:
                    return GdaxOrderSide.Sell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        public TradeType GdaxOrderSideToTradeType(GdaxOrderSide orderSide)
        {
            switch (orderSide)
            {
                case GdaxOrderSide.Buy:
                    return TradeType.Buy;
                case GdaxOrderSide.Sell:
                    return TradeType.Sell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderSide), orderSide, null);
            }
        }

        public ExecutionStatus GdaxOrderStatusToExecutionStatus(GdaxOrderResponse order)
        {
            switch (order.Status)
            {
                case "open":
                    return ExecutionStatus.New;
                case "pending":
                    return ExecutionStatus.Pending;
                case "active":  // Is this correct - Investigate
                    return ExecutionStatus.PartialFill;
                case "cancelled":  // do we have such status? Investigate
                    return ExecutionStatus.Cancelled;
                case "done":
                    return ExecutionStatus.Fill;
            }

            return ExecutionStatus.Unknown;
        }

        public AccountBalance GdaxBalanceToAccountBalance(GdaxBalanceResponse gdaxBalance)
        {
            return new AccountBalance
            {
                Asset = gdaxBalance.Currency,
                Balance = gdaxBalance.Balance
            };
        }
    }
}
