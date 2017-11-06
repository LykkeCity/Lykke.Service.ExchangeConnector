using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace TradingBot.Repositories
{
    public sealed class OrderBookEntity: TableEntity
    {
        public DateTime SnapshotDateTime { get; }

        public string Source { get; }

        public string AssetPair { get; }

        public string Asks { get; }

        public string Bids { get; }

        public OrderBookEntity()
        {
        }

        public OrderBookEntity(string source, string assetPair,
            DateTime snapShotTimestamp, string asks, string bids)
        {
            SnapshotDateTime = snapShotTimestamp;
            Source = source;
            AssetPair = assetPair;
            Asks = asks;
            Bids = bids;

            PartitionKey = source + assetPair;
            RowKey = snapShotTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }
    }
}
