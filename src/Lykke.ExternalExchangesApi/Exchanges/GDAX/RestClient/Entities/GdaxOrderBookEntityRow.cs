using System;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public sealed class GdaxOrderBookEntityRow
    {
        public Guid OrderId { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        public override string ToString()
        {
            var text = $"ProductID: {OrderId}, Amount: {Size}, Price: {Price}";
            return text;
        }
    }
}
