using AzureStorage;
using Common;
using Common.Log;
using Lykke.Service.ExchangeDataStore.Core.Domain;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Helpers;
using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.AzureRepositories.OrderBooks
{
    public class OrderBookRepository : IOrderBookRepository
    {
        private static readonly string _className = nameof(OrderBookRepository);

        private readonly ILog _log;
        private readonly INoSQLTableStorage<OrderBookSnapshotEntity> _tableStorage;
        private readonly string _blobContainer;
        private readonly IBlobStorage _blobStorage;

        public OrderBookRepository(INoSQLTableStorage<OrderBookSnapshotEntity> tableStorage, IBlobStorage blobStorage, ILog log, AzureTableConfiguration dbConfig)
        {
            _tableStorage = tableStorage;
            _blobStorage = blobStorage;
            _log = log;
            _blobContainer = dbConfig.EntitiesBlobContainerName;
            
        }

        public async Task<List<OrderBook>> GetAsync(string exchangeName, string instrument, DateTime from, DateTime to, CancellationToken cancelToken)
        {
            await _log.WriteInfoAsync(_className, nameof(GetAsync), $"{exchangeName}, {instrument}, {from}, {to}");

            var partitionKey = $"{exchangeName}_{instrument}".RemoveSpecialCharacters('-', '_', '.');
          
            var blobNames = (await _tableStorage.GetDataAsync(partitionKey, s => s.RowKey.ParseOrderbookTimestamp() >= from && s.RowKey.ParseOrderbookTimestamp() <= to)).Select(s => s.UniqueId).OrderBy(r => r).ToList();

            var result = new ConcurrentBag<OrderBook>();
            

            //there is no way of downloading multiple blobs with a single request (e.g. batch download). So we try to download them in parallel with multiple simultaneous requests, each handling a batch of blobs.
            Parallel.ForEach(blobNames, new ParallelOptions() { CancellationToken = cancelToken, MaxDegreeOfParallelism = Constants.MaxDegreeOfParallelismForBlobsDownload }, 
            (blobName) =>
            {
                try
                {
                    cancelToken.ThrowIfCancellationRequested();

                    var ordersStream = _blobStorage.GetAsync(_blobContainer, blobName).Result;
                    var orders = ToListOfOrderBookItems(ordersStream.ToBytes());

                    var asks = new ReadOnlyCollection<VolumePrice>(orders.Where(s => !s.IsBuy).Select(a => new VolumePrice(a.Price, a.Size)).ToList());
                    var bids = new ReadOnlyCollection<VolumePrice>(orders.Where(s => s.IsBuy).Select(a => new VolumePrice(a.Price, a.Size)).ToList());

                    Regex matchTimeStamp = new Regex(@"[0-9.T]+$");//extract orderbook timestamp from name
                    var match = matchTimeStamp.Match(blobName);
                    if (match.Success)
                    {
                        var orderBookTimestamp = DateTime.ParseExact(match.Value, Constants.OrderbookTimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

                        var orderBook = new OrderBook(exchangeName, instrument, asks, bids, orderBookTimestamp);
                        result.Add(orderBook);
                    }
                    else
                    {
                        throw new ApplicationException($"Can not extract timestamp from blob name {blobName}.");
                    }
                }
                catch (Exception ex)
                {
                    _log.WriteWarningAsync(_className, blobName, ex.Message);
                }
            });

            await _log.WriteInfoAsync(_className, nameof(GetAsync), $"{result.Count} results found for {exchangeName}, {instrument}, {from}, {to}");
            return result.ToList();
        }

        private static readonly JsonSerializerSettings DeserializeSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        private List<OrderBookItem> ToListOfOrderBookItems(byte[] data)
        {
            return JsonConvert.DeserializeObject<List<OrderBookItem>>(Encoding.UTF8.GetString(data), DeserializeSettings);
        }
    }
}
