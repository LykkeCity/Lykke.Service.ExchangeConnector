using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace TradingBot.Repositories
{
    public class JavaIntrinsicEventEntity : TableEntity
    {
        public string Exchange { get; set; }
        public string Instrument { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Liquidity { get; set; }
        public string CoastLineTraderId { get; set; }
        public int ProperRunnerIndex { get; set; }
        public int Runner0event { get; set; }
        public int Runner1event { get; set; }
        public int Runner2event { get; set; }
        public double BuyLimitOrderPrice { get; set; }
        public double BuyLimitOrderVolume { get; set; }
        public string BuyLimitOrderId { get; set; }
        public double SellLimitOrderPrice { get; set; }
        public double SellLimitOrderVolume { get; set; }
        public string SellLimitOrderId { get; set; }
        public double Inventory { get; set; }
        public DateTime Datetime { get; set; }
        public int LongShort { get; set; }
    }
}
