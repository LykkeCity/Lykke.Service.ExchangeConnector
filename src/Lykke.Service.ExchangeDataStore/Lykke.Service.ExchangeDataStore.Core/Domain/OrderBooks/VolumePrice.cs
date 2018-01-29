using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks
{
    public sealed class VolumePrice
    {
        public VolumePrice(decimal price, decimal volume)
        {
            Price = price;
            Volume = Math.Abs(volume);
        }

        [JsonProperty("price")]
        public decimal Price { get; }

        [JsonProperty("volume")]
        public decimal Volume { get; }

    }
}
