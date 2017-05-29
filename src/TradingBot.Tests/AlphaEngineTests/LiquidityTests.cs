using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TradingBot.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class LiquidityTests
    {
        [Fact]
        public void RealData_EURCHF_2011_Test()
        {
            var network = new IntrinsicNetwork(12, 0.00025m, TimeSpan.FromMinutes(10));
            
            var path = AppContext.BaseDirectory + "/../../../Data/DAT_MT_EURCHF_M1_2011.csv";
            using (var reader = new HistoricalDataReader(path, LineParsers.ParseMTLine))
                foreach(var priceTime in reader)
                {
                    network.OnPriceChange(priceTime);
                }

            File.WriteAllLines(AppContext.BaseDirectory + "/../../../Data/EURCHF_M1_2011_Liquidities.csv",
                network.Liquidities.Select(x => $"{x.Time}\t{x.Value}"));
        }

        [Fact]
        public void RealData_EURCHF_2011_Ticks_Test()
        {
            var network = new IntrinsicNetwork(12, 0.00025m, TimeSpan.FromDays(1));

            var paths =
                Enumerable.Range(1, 31)
                .Select(x => $"EURCHF_2011_07_{x:00}.csv")
                .Concat(
                    Enumerable.Range(1, 31)
                    .Select(x => $"EURCHF_2011_08_{x:00}.csv")
                )
                .Concat(
                    Enumerable.Range(1, 30)
                    .Select(x => $"EURCHF_2011_09_{x:00}.csv")
                )
                .Select(x => AppContext.BaseDirectory + "/../../../../../../FxData/EURCHF_2011/" + x)
                .ToArray();
            
            using (var reader = new HistoricalDataReader(paths, LineParsers.ParseTickLine))
                foreach (var priceTime in reader)
                {
                    network.OnPriceChange(priceTime);
                }


            var lines = new List<string>();

            var startTime = new DateTime(2011, 07, 01, 0, 0, 0);
            var endTime = new DateTime(2011, 09, 30, 23, 59, 59);
            for (DateTime time = startTime; time <= endTime; time = time.AddMinutes(1))
            {
                var liquidity = network.Liquidities.Last(x => x.Time <= time);

                var line = $"{time}\t{liquidity.Value}";

                lines.Add(line);
            }

            File.WriteAllLines(AppContext.BaseDirectory + "/../../../Data/EURCHF_2011_08_Liquidities.csv",
                //network.Liquidities.Select(x => $"{x.Time}\t{x.Value}")
                lines);
        }

        [Fact]
        public void RealData_USDJPY_2007_Ticks_Test()
        {
            var network = new IntrinsicNetwork(12, 0.00025m, TimeSpan.FromDays(1));

            var paths = new List<string>();

            var startDay = new DateTime(2007, 05, 01);
            var endDay = new DateTime(2007, 08, 31);
            for (DateTime day = startDay; day <= endDay; day = day.AddDays(1))
            {
                var fileName = $"USDJPY_{day.ToString("yyyy_MM_dd")}.csv";
                paths.Add(
                    AppContext.BaseDirectory + "/../../../../../../FxData/USDJPY_2007/" + fileName);
            }

            using (var reader = new HistoricalDataReader(paths.ToArray(), LineParsers.ParseTickLine))
                foreach (var priceTime in reader)
                {
                    network.OnPriceChange(priceTime);
                }


            var lines = new List<string>();

            var startTime = new DateTime(2007, 05, 02, 0, 0, 0);
            var endTime = new DateTime(2007, 08, 31, 23, 59, 59);
            for (DateTime time = startTime; time <= endTime; time = time.AddMinutes(1))
            {
                var liquidity = network.Liquidities.Last(x => x.Time <= time);

                var line = $"{time}\t{liquidity.Value}";

                lines.Add(line);
            }

            File.WriteAllLines(AppContext.BaseDirectory + "/../../../Data/USDJPY_2007_Liquidities.csv",
                //network.Liquidities.Select(x => $"{x.Time}\t{x.Value}")
                lines);
        }
    }
}
