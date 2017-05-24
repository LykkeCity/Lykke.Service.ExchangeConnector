using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TradingBot.AlphaEngine;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.Trading;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class AlphaEngineTestsOnData
    {
        private class HistoricalDataReader : IDisposable, IEnumerable<PriceTime>
        {
            public HistoricalDataReader()
            {
                var path = AppContext.BaseDirectory + "/../../../Data/EUR_USD.csv";
                reader = File.OpenText(path);
            }

            private StreamReader reader;

            public void Dispose()
            {
                reader.Dispose();
            }

            public IEnumerator<PriceTime> GetEnumerator()
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineSplit = line.Split(',');

                    DateTime time = DateTime.Parse(lineSplit[1]);
                    decimal price = decimal.Parse(lineSplit[2], System.Globalization.CultureInfo.InvariantCulture);

                    yield return new PriceTime(price, time);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public void HistoricalDataTest()
        {
            var instrument = new Instrument("EUR_USD");
            var threshold = 0.002m;

            var intrinsicTime = new IntrinsicTime(threshold);
            var coastlineTrader = new CoastlineTrader(instrument, intrinsicTime);

            using(var reader = new HistoricalDataReader())
            foreach(var priceTime in reader)
            {
                coastlineTrader.OnPriceChange(priceTime);
            }
            
            Console.WriteLine("DownwardDirectionalChangesCount: " + intrinsicTime.IntrinsicTimeEvents.OfType<DirectionalChange>().Count(x => x.Mode == AlgorithmMode.Down));
            Console.WriteLine("UpwardDirectionalChangesCount: " + intrinsicTime.IntrinsicTimeEvents.OfType<DirectionalChange>().Count(x => x.Mode == AlgorithmMode.Up));

            Console.WriteLine("UpwardOvershootsCount: " + intrinsicTime.IntrinsicTimeEvents.OfType<Overshoot>().Count(x => x.Mode == AlgorithmMode.Up));
            Console.WriteLine("DownwardOvershootsCount: " + intrinsicTime.IntrinsicTimeEvents.OfType<Overshoot>().Count(x => x.Mode == AlgorithmMode.Down));

            var tuple = intrinsicTime.GetAvaregesForDcAndFollowedOs();

            Assert.True(coastlineTrader.Position.Money > 0);
        }

        [Fact]
        public async Task TestOnKraken()
        {            
            var instrument = new Instrument("XXBTZUSD");
            var threshold = 0.002m;

            var intrinsicTime = new IntrinsicTime(threshold);
            var coastlineTrader = new CoastlineTrader(instrument, intrinsicTime);

            var valuesFromKraken = await new PublicData(new ApiClient(new HttpClient())).GetOHLC(instrument.Name);
            
            foreach (var item in valuesFromKraken.Data[instrument.Name])
            {
                coastlineTrader.OnPriceChange(new PriceTime(item.Open, item.Time));
            }


            var tuple = intrinsicTime.GetAvaregesForDcAndFollowedOs();
            
            Assert.True(coastlineTrader.Position.Money > 0);
        }

        [Fact]
        public void TestIntrinsicNetwork()
        {
            var network = new IntrinsicNetwork(5, .00025m, .0005m, .001m, .002m, .004m);
            network.Init();

            var states = new StringBuilder();
            string prevState = "";

            using (var reader = new HistoricalDataReader())
                foreach(var priceTime in reader)
                {
                    network.OnPriceChange(priceTime);
                    var state = network.GetStateString();
                    if (state != prevState)
                    {
                        states.AppendLine(state);
                        prevState = state;
                    }
                }

            Console.Write(states);
        }

        [Fact]
        public void TestAlphaEngineAgent()
        {
            var agent = new AlphaEngineAgent(new Instrument("EUR_USD"));

            using (var reader = new HistoricalDataReader())
                foreach (var priceTime in reader)
                {
                    agent.OnPriceChange(priceTime);
                }

            Assert.True(agent.GetCumulativePosition().Money > 0);
        }
    }
}
