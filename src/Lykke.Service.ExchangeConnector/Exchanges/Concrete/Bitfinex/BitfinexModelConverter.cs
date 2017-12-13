using System.Globalization;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    public static class BitfinexModelConverter
    {
        public static OrderBookItem ToOrderBookItem(OrderBookItemResponse response)
        {
            return new OrderBookItem
            {
                Id = response.Id.ToString(CultureInfo.InvariantCulture),
                IsBuy = response.Amount > 0,
                Price = response.Price,
                Symbol = response.Pair,
                Size = response.Amount
            };
        }
    }
}
