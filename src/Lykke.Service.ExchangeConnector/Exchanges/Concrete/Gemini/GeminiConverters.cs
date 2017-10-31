using System;
using System.Linq;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities;

namespace TradingBot.Exchanges.Concrete.Gemini
{
    internal class GeminiConverters
    {
        private readonly GeminiExchangeConfiguration _configuration;
        private readonly string _exchangeName;

        public GeminiConverters(GeminiExchangeConfiguration configuration, string exchangeName)
        {
            _configuration = configuration;
            _exchangeName = exchangeName;
        }

        public ExecutedTrade OrderToTrade(GeminiOrderResponse order)
        {
            var id = order.Id;
            var execTime = order.CreatedAt;
            var execPrice = order.Price;
            var execVolume = order.ExecutedValue;
            var tradeType = GeminiOrderSideToTradeType(order.Side);
            var status = GeminiOrderStatusToExecutionStatus(order);
            var instr = LykkeSymbolToGeminiInstrument(order.ProductId);

            return new ExecutedTrade(instr, execTime, execPrice, execVolume,
                tradeType, id.ToString(), status);
        }

        public string LykkeSymbolToGeminiSymbol(string symbol)
        {
            if (!_configuration.CurrencyMapping.TryGetValue(symbol, out var result))
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to {GeminiExchange.Name} value");
            }
            return result;
        }

        public Instrument LykkeSymbolToGeminiInstrument(string symbol)
        {
            var result = _configuration.CurrencyMapping.FirstOrDefault(kv => kv.Value == symbol).Key;
            if (result == null)
            {
                throw new ArgumentException($"Symbol {symbol} is not mapped to lykke value");
            }
            return new Instrument(_exchangeName, result);
        }

        public GeminiOrderType OrderTypeToGeminiOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:
                    return GeminiOrderType.Market;
                case OrderType.Limit:
                    return GeminiOrderType.Limit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public GeminiOrderSide TradeTypeToGeminiOrderSide(TradeType signalTradeType)
        {
            switch (signalTradeType)
            {
                case TradeType.Buy:
                    return GeminiOrderSide.Buy;
                case TradeType.Sell:
                    return GeminiOrderSide.Sell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(signalTradeType), signalTradeType, null);
            }
        }

        public TradeType GeminiOrderSideToTradeType(GeminiOrderSide orderSide)
        {
            switch (orderSide)
            {
                case GeminiOrderSide.Buy:
                    return TradeType.Buy;
                case GeminiOrderSide.Sell:
                    return TradeType.Sell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderSide), orderSide, null);
            }
        }

        public ExecutionStatus GeminiOrderStatusToExecutionStatus(GeminiOrderResponse order)
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

        public AccountBalance GeminiBalanceToAccountBalance(GeminiBalanceResponse geminiBalance)
        {
            return new AccountBalance
            {
                Asset = geminiBalance.Currency,
                Balance = geminiBalance.Balance
            };
        }
    }
}
