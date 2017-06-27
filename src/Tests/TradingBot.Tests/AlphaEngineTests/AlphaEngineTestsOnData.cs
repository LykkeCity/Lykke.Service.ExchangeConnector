using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class AlphaEngineTestsOnData
    {
        private class HistoricalDataReader : IDisposable, IEnumerable<TickPrice>
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

            public IEnumerator<TickPrice> GetEnumerator()
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineSplit = line.Split(',');

                    DateTime time = DateTime.Parse(lineSplit[1]);
                    decimal price = decimal.Parse(lineSplit[2], System.Globalization.CultureInfo.InvariantCulture);

                    yield return new TickPrice(time, price);
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

            var valuesFromKraken = await new PublicData(new ApiClient(new HttpClient())).GetOHLC(CancellationToken.None, instrument.Name);
            
            foreach (var item in valuesFromKraken.Data[instrument.Name])
            {
                coastlineTrader.OnPriceChange(new TickPrice(item.Time, item.Close));
            }


            var tuple = intrinsicTime.GetAvaregesForDcAndFollowedOs();
            
            Assert.True(coastlineTrader.Position.Money > 0);
        }

        [Fact]
        public void TestIntrinsicNetwork()
        {
            var network = new IntrinsicNetwork(5, .00025m, TimeSpan.FromMinutes(10));

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
