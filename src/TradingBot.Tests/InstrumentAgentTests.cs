using System;
using System.IO;
using TradingBot.AlphaEngine;
using Xunit;

namespace TradingBot.Tests
{
    public class InstrumentAgentTests
    {
        [Fact]
        public void HistoricalDataTest()
        {
            var path = AppContext.BaseDirectory + "/../../../Data/EUR_USD.csv";
            var rdr = File.OpenText(path);

            var threshold = 0.002m;
            var agent = new InstrumentAgent("EUR_USD", threshold);

            while(!rdr.EndOfStream)
            {
                var line = rdr.ReadLine();
                var lineSplit = line.Split(',');

                DateTime time = DateTime.Parse(lineSplit[1]);
                decimal price = decimal.Parse(lineSplit[2], System.Globalization.CultureInfo.InvariantCulture);

                agent.HandlePriceChange(price, time);
            }

            

            Console.WriteLine(agent.DirectionalChangesToDown);
            Console.WriteLine(agent.DirectionalChangesToUp);

            var tuple = agent.GetAvaregesForDcAndFollowedOs();


        }
    }
}
