using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.Kraken.Requests
{
    public class CancelOrderRequest : IKrakenRequest
    {
        public CancelOrderRequest(string txId)
        {
            TxId = txId;
        }
        
        public string TxId { get; set; }

        public IEnumerable<KeyValuePair<string, string>> FormData
        {
            get
            {
                yield return new KeyValuePair<string, string>("txid", TxId);
            }
        }
    }
}