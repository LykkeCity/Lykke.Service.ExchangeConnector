// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks
{
    public class OrderBookItem 
    {
        public decimal Price { get; set; }

        public decimal Size { get; set; }

        public string Symbol { get; set; }

        public bool IsBuy { get; set; }
    }
}
