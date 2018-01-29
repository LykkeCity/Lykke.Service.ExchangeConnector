using System;
using Common.Log;

namespace Lykke.Service.ExchangeDataStore.Client
{
    public class ExchangeDataStoreClient : IExchangeDataStoreClient, IDisposable
    {
        private readonly ILog _log;

        public ExchangeDataStoreClient(string serviceUrl, ILog log)
        {
            _log = log;
        }

        public void Dispose()
        {
            //if (_service == null)
            //    return;
            //_service.Dispose();
            //_service = null;
        }
    }
}
