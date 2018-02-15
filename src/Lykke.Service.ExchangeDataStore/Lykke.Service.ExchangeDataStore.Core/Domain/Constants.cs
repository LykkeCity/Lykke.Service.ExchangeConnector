namespace Lykke.Service.ExchangeDataStore.Core.Domain
{
    public static class Constants
    {
        public const string JsonSerializationTimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";
        public const string OrderbookTimestampFormat = "yyyyMMddTHHmmss.fff";
        public const int MaxDegreeOfParallelismForBlobsDownload = 15;
        public const int BlobStorageTimeOutSeconds = 400;
    }
}
