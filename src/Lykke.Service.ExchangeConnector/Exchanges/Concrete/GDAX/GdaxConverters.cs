using System;
using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using OrderType = TradingBot.Trading.OrderType;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal class GdaxConverters: ExchangeConverters
    {
        public GdaxConverters(IReadOnlyCollection<CurrencySymbol> currencySymbols, 
            string exchangeName): base(currencySymbols, exchangeName)
        {

        }

        public ExecutedTrade OrderToTrade(GdaxOrderResponse order)
        {
            var id = order.Id;
            var execTime = order.CreatedAt;
            var execPrice = order.Price;
            var execVolume = order.ExecutedValue;
            var tradeType = GdaxOrderSideToTradeType(order.Side);
            var status = GdaxOrderStatusToExecutionStatus(order);
            var instr = ExchangeSymbolToLykkeInstrument(order.ProductId);

            return new ExecutedTrade(instr, execTime, execPrice, execVolume,
                tradeType, id.ToString(), status);
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

        public Shared.OrderBookItem GdaxOrderBookItemToOrderBookItem(string symbol, bool isBuy, 
            GdaxOrderBookEntityRow gdaxItem)
        {
            return new Shared.OrderBookItem
            {
                Id = gdaxItem.OrderId.ToString(),
                IsBuy = isBuy,
                Symbol = symbol,
                Price = gdaxItem.Price,
                Size = gdaxItem.Size
            };
        }
    }
}
