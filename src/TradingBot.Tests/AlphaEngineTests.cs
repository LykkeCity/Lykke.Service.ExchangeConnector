using System;
using System.IO;
using TradingBot.AlphaEngine;
using TradingBot.Trading;
using Xunit;

namespace TradingBot.Tests
{
    public class AlphaEngineTests
    {
        [Fact]
        public void HistoricalDataTest()
        {
            var path = AppContext.BaseDirectory + "/../../../Data/EUR_USD.csv";
            var rdr = File.OpenText(path);

            var instrument = "EUR_USD";
            var threshold = 0.002m;

            var engineAgent = new IntrinsicTime(instrument, threshold);
            var tradingAgent = new TradingAgent(instrument);
            engineAgent.NewIntrinsicTimeEventGenerated += tradingAgent.HandleEvent;

            var position = new Position(instrument);
            tradingAgent.NewSignalGenerated += position.AddSignal;

            while(!rdr.EndOfStream)
            {
                var line = rdr.ReadLine();
                var lineSplit = line.Split(',');

                DateTime time = DateTime.Parse(lineSplit[1]);
                decimal price = decimal.Parse(lineSplit[2], System.Globalization.CultureInfo.InvariantCulture);

                engineAgent.HandlePriceChange(price, time);
            }
            
            Console.WriteLine("DownwardDirectionalChangesCount: " + engineAgent.DownwardDirectionalChangesCount);
            Console.WriteLine("UpwardDirectionalChangesCount: " + engineAgent.UpwardDirectionalChangesCount);

            Console.WriteLine("UpwardOvershootsCount: " + engineAgent.UpwardOvershootsCount);
            Console.WriteLine("DownwardOvershootsCount: " + engineAgent.DownwardOvershootsCount);

            var tuple = engineAgent.GetAvaregesForDcAndFollowedOs();

            Assert.True(position.Amount > 0);
        }
    }
}
