using System;
using TradingBot.Helpers;

namespace TradingBot.Repositories
{
    public sealed class OrderBookSnapshotEntity: BaseEntity
    {
        public string UniqueId => $"{PartitionKey}_{RowKey}";

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

            PartitionKey = $"{exchange}_{assetPair}".RemoveSpecialCharacters('-', '_', '.');
            RowKey = snapShotTimestamp.ToString("yyyyMMddTHHmmss.fff");
        }
    }
}
