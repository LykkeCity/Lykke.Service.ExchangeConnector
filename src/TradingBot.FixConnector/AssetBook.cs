using System;
using System.Collections.Generic;

namespace TradingBot.FixConnector
{
    public class AssetBook
    {
        public AssetBook()
        {
        }

        public string Asset { get; set; }

        public Dictionary<string, Quote> QuotesMap { get; set; }

        public List<Quote> Bid { get; set; }

        public List<Quote> Ask { get; set; }
    }
}
