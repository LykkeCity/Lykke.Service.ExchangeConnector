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
        public static readonly string Name = "lykke";
        private readonly IHandler<TradingOrderBook> _rabbitMqHandler;
        private readonly Dictionary<string, LykkeOrderBook> _halfOrderBooks = new Dictionary<string, LykkeOrderBook>();

        public OrderBookHandlerDecorator(IHandler<TradingOrderBook> rabbitMqHandler)
        {
            _rabbitMqHandler = rabbitMqHandler;
        }

        public async Task Handle(LykkeOrderBook message)
        {
            // Update current order book
            var currentKey = message.AssetPair + message.IsBuy;
            _halfOrderBooks[currentKey] = message;

            // Find a pair and send it
            var wantedKey = message.AssetPair + !message.IsBuy;
            if (_halfOrderBooks.TryGetValue(wantedKey, out var otherHalf))
            {
                var fullOrderBook = CreateOrderBook(Name, message, otherHalf);
                await _rabbitMqHandler.Handle(fullOrderBook);
            }
        }

        private TradingOrderBook CreateOrderBook(string exchangeName, LykkeOrderBook one, LykkeOrderBook another)
        {
            if (one.AssetPair != another.AssetPair)
                throw new ArgumentException($"{nameof(one)}.AssetPair != {nameof(another)}.AssetPair");

            if (one.IsBuy == another.IsBuy)
                throw new ArgumentException($"{nameof(one)}.IsBuy == {nameof(another)}.IsBuy");

            var source = exchangeName;
            var assetPair = one.AssetPair;
            var timestamp = one.Timestamp;

            var onePrices = one.Prices.Select(x => new VolumePrice(x.Price, x.Volume)).ToList();
            var anotherPrices = another.Prices.Select(x => new VolumePrice(x.Price, x.Volume)).ToList();

            var bids = one.IsBuy ? onePrices : anotherPrices;
            var asks = !one.IsBuy ? onePrices : anotherPrices;

            return new TradingOrderBook(source, assetPair, asks, bids, timestamp);
        }
    }
}
