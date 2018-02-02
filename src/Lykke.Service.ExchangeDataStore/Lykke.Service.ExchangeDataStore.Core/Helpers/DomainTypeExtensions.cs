using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;

namespace Lykke.Service.ExchangeDataStore.Core.Helpers
{
    public static class DomainTypeExtensions
    {
        public static OrderBookItem ToAskOrderBookItem(this VolumePrice order, string assetPairId)
        {
            return new OrderBookItem
            {
                //Ignore Id,
                Size = order.Volume * -1,
                Price = order.Price,
                Symbol = assetPairId,
                IsBuy = false
            };
        }
        public static OrderBookItem ToBidOrderBookItem(this VolumePrice order, string assetPairId)
        {
            return new OrderBookItem
            {
                //Ignore Id,
                Size = order.Volume,
                Price = order.Price,
                Symbol = assetPairId,
                IsBuy = true
            };
        }
    }
}
