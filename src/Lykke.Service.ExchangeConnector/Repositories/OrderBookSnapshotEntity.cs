using System;
using Microsoft.WindowsAzure.Storage.Table;
using TradingBot.Helpers;

namespace TradingBot.Repositories
{
    public sealed class OrderBookSnapshotEntity: TableEntity
    {
        public string UniqueId => PartitionKey + RowKey;

        public string Exchange { get; }

        public string AssetPair { get; }

        public DateTime SnapshotDateTime { get; }

        public OrderBookSnapshotEntity()
        {
        }

        public OrderBookSnapshotEntity(string exchange, string assetPair, DateTime snapShotTimestamp)
        {
            SnapshotDateTime = snapShotTimestamp;
            Exchange = exchange;
            AssetPair = assetPair;

            PartitionKey = (exchange + assetPair).RemoveSpecialCharacters('-', '_', '.');
            RowKey = snapShotTimestamp.ToString("yyyy-MM-dd_HH-mm-ss.fff");
        }
    }
}
