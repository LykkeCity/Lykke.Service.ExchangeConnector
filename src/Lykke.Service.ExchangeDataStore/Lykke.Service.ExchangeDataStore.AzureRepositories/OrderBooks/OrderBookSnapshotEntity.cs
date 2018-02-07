using Lykke.AzureStorage.Tables;
using Lykke.Service.ExchangeDataStore.Core.Helpers;
using System;
using System.Threading;

namespace Lykke.Service.ExchangeDataStore.AzureRepositories.OrderBooks
{
    public sealed class OrderBookSnapshotEntity: AzureTableEntity
    {
        private static int _rowKeySuffix = 0;

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
            RowKey = snapShotTimestamp.ToSnapshotTimestampFormat() + Interlocked.Increment(ref _rowKeySuffix); 
        }
    }
}
