using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Trading;
using TradingOrderBook = TradingBot.Trading.OrderBook;
using LykkeOrderBook = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.OrderBook;

namespace TradingBot.Handlers
{
    internal class OrderBookHandlerDecorator : IHandler<LykkeOrderBook>
    {
        private const string Name = "lykke";
        private readonly IHandler<TradingOrderBook> _rabbitMqHandler;
        private readonly Dictionary<string, LykkeOrderBook> _halfOrderBooks = new Dictionary<string, LykkeOrderBook>();

        public OrderBookHandlerDecorator(IHandler<TradingOrderBook> rabbitMqHandler)
        {
            _rabbitMqHandler = rabbitMqHandler;
        }

        public async Task Handle(LykkeOrderBook message)
        {
            // Update current half of the order book
            var currentKey = message.AssetPair + message.IsBuy;
            _halfOrderBooks[currentKey] = message;

            // Find a pair half and send it
            var wantedKey = message.AssetPair + !message.IsBuy;
            if (_halfOrderBooks.TryGetValue(wantedKey, out var otherHalf))
            {
                var fullOrderBook = CreateOrderBook(message, otherHalf);

                // If bestAsk < bestBid then ignore the order book as outdated
                if (fullOrderBook.Asks.Any() && fullOrderBook.Bids.Any() && fullOrderBook.Asks.Min(x => x.Price) < fullOrderBook.Bids.Max(x => x.Price))
                    return;

                await _rabbitMqHandler.Handle(fullOrderBook);
            }
        }

        private TradingOrderBook CreateOrderBook(LykkeOrderBook one, LykkeOrderBook another)
        {
            if (one.AssetPair != another.AssetPair)
                throw new ArgumentException($"{nameof(one)}.{nameof(one.AssetPair)} != {nameof(another)}.{nameof(another.AssetPair)}");

            if (one.IsBuy == another.IsBuy)
                throw new ArgumentException($"{nameof(one)}.{nameof(one.IsBuy)} == {nameof(another)}.{nameof(another.IsBuy)}");
            
            var assetPair = one.AssetPair;
            var timestamp = one.Timestamp;

            var onePrices = one.Prices.Select(x => new VolumePrice(x.Price, x.Volume)).ToList();
            var anotherPrices = another.Prices.Select(x => new VolumePrice(x.Price, x.Volume)).ToList();

            var bids = one.IsBuy ? onePrices : anotherPrices;
            var asks = !one.IsBuy ? onePrices : anotherPrices;

            var result = new TradingOrderBook(Name, assetPair, asks, bids, timestamp);

            return result;
        }
    }
}
