using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.Kraken.Requests
{
    public class TradeBalanceRequest : IKrakenRequest
    {
        public string AssetClass { get; set; }
        
        public string BaseAsset { get; set; }

        public IEnumerable<KeyValuePair<string, string>> FormData
        {
            get
            {
                if (!string.IsNullOrEmpty(AssetClass))
                    yield return new KeyValuePair<string, string>("aclass", AssetClass);

                if (!string.IsNullOrEmpty(BaseAsset))
                    yield return new KeyValuePair<string, string>("asset", BaseAsset);
            }
        }
    }
}