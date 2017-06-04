using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TradingBot.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class LiquidityTests
    {
        [Fact]
        public void RealData_EURCHF_2011_Ticks_Test()
        {
            var network = new IntrinsicNetwork(12, 0.00025m, TimeSpan.FromDays(1));

            var paths = new List<string>();

            var startDay = new DateTime(2011, 07, 01);
            var endDay = new DateTime(2011, 09, 30);
            for (DateTime day = startDay; day <= endDay; day = day.AddDays(1))
            {
                var fileName = $"EURCHF_{day.ToString("yyyy_MM_dd")}.csv";
                paths.Add(
                    AppContext.BaseDirectory + "/../../../../../../FxData/EURCHF_2011/" + fileName);
            }

            List<string> outputLines = new List<string>(500000);
            Liquidity prevLiquidity = null;

            using (var reader = new HistoricalDataReader(paths.ToArray(), LineParsers.ParseTickLine))
            {
                TimeSpan outputResolution = TimeSpan.FromSeconds(30);
                DateTime previousOutput = new DateTime();

                //foreach (var priceTime in reader)
                reader.ForEach(priceTime =>
                {
                    var liquidity = network.OnPriceChange(priceTime) ?? prevLiquidity;

                    if (priceTime.Time - previousOutput > outputResolution)
                    {
                        outputLines.Add($"{priceTime.Time.ToString("dd.MM.yyyy HH:mm:ss,fff", CultureInfo.InvariantCulture)}\t" +
                              $"{priceTime.Mid}\t{liquidity?.Value}");
                        previousOutput = priceTime.Time;
                    }

                    prevLiquidity = liquidity;
                });
            }

            File.WriteAllLines(AppContext.BaseDirectory + "/../../../Data/EURCHF_2011_Liquidities.csv", outputLines);
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

            List<string> outputLines = new List<string>(500000);
            Liquidity prevLiquidity = null;

            using (var reader = new HistoricalDataReader(paths.ToArray(), LineParsers.ParseTickLine))
            {
                TimeSpan outputResolution = TimeSpan.FromSeconds(30);
                DateTime previousOutput = new DateTime();

                //foreach (var priceTime in reader)
                reader.ForEach(priceTime => 
                {
                    var liquidity = network.OnPriceChange(priceTime) ?? prevLiquidity;

                    if (priceTime.Time - previousOutput > outputResolution)
                    {
                        outputLines.Add($"{priceTime.Time.ToString("dd.MM.yyyy HH:mm:ss,fff", CultureInfo.InvariantCulture)}\t" +
                              $"{priceTime.Mid}\t{liquidity?.Value}");
                        previousOutput = priceTime.Time;
                    }

                    prevLiquidity = liquidity;
                });
            }
            
            File.WriteAllLines(AppContext.BaseDirectory + "/../../../Data/USDJPY_2007_Liquidities.csv", outputLines);
        }
    }
}
