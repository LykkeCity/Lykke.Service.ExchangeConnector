using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace TradingBot.Repositories
{
    public sealed class OrderBookEntity: TableEntity
    {
        public DateTime SnapshotDateTime { get; }

        public string Source { get; }

        public string AssetPair { get; }

        public OrderBookEntity()
        {
        }

        public OrderBookEntity(string source, string assetPair, DateTime snapShotTimestamp)
        {
            SnapshotDateTime = snapShotTimestamp;
            Source = source;
            AssetPair = assetPair;

            PartitionKey = source + assetPair;
            RowKey = snapShotTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }
    }
}
