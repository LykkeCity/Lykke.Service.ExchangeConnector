using Lykke.Service.ExchangeDataStore.Core.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks
{
    public sealed class OrderBook
    {
        public OrderBook(string source, string assetPairId, IReadOnlyCollection<VolumePrice> asks, IReadOnlyCollection<VolumePrice> bids, DateTime timestamp)
        {
            Source = source;
            AssetPairId = assetPairId;
            Asks = asks;
            Bids = bids;
            Timestamp = timestamp;
        }

        [JsonProperty("source")]
        public string Source { get; }

        [JsonProperty("asset")]
        public string AssetPairId { get; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; }

        [JsonProperty("asks")]
        public IReadOnlyCollection<VolumePrice> Asks { get; }

        [JsonProperty("bids")]
        public IReadOnlyCollection<VolumePrice> Bids { get; }

        public string Info()
        {
            return $"{Source},{AssetPairId},{Timestamp.ToSnapshotTimestampFormat()}";
        }

    }

    
}
