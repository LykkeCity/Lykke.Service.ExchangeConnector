using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TradingBot.Trading;

namespace TradingBot.Tests
{
    public class HistoricalDataReader : IDisposable, IEnumerable<TickPrice>
    {
        public HistoricalDataReader(string[] paths, Func<string, TickPrice> lineParser)
        {
            this.paths = paths;
            this.lineParser = lineParser;
        }

        public HistoricalDataReader(string path, Func<string, TickPrice> lineParser)
            : this(new[] { path }, lineParser)
        {
        }
        
        private string[] paths;

        //private StreamReader reader;

        private Func<string, TickPrice> lineParser;
        

        public void Dispose()
        {
            //reader?.Dispose();
        }

        public IEnumerator<TickPrice> GetEnumerator()
        {
            for (var i = 0; i < paths.Length; i++)
            {
                //reader = File.OpenText(paths[i]);

                var lines = File.ReadAllLines(paths[i]);
                
                //while (!reader.EndOfStream)

                foreach(var line in lines)
                {
                    //var line = reader.ReadLine();

                    var priceTime = lineParser(line);
                    if (priceTime == null) continue;

                    yield return priceTime;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ForEach(Action<TickPrice> action)
        {
            for (var i = 0; i < paths.Length; i++)
            {
                var lines = File.ReadAllLines(paths[i]);
                
                foreach (var line in lines)
                {
                    var priceTime = lineParser(line);
                    if (priceTime == null) continue;

                    action(priceTime);
                }
            }
        }
    }

    public class LineParsers
    {
        /// <summary>
        /// Parse string: 2011.01.02,17:00,1.247600,1.247600,1.247600,1.247600,0
        /// </summary>
        public static TickPrice ParseMTLine(string line)
        {
            var columns = line.Split(',');
            var time = DateTime.Parse($"{columns[0]} {columns[1]}");
            var price = decimal.Parse(columns[2], CultureInfo.InvariantCulture);

            return new TickPrice(new Instrument("", ""), time, price);
        }

        /// <summary>
        /// Parse string: 20110731 235958;1.138900;0
        /// </summary>
        public static TickPrice ParseNTLine(string line)
        {
            var columns = line.Split(';');

            var time = DateTime.ParseExact(columns[0], "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);
            var price = decimal.Parse(columns[1], CultureInfo.InvariantCulture);

            return new TickPrice(new Instrument("", ""), time, price);
        }

        /// <summary>
        /// Prase file such as:
        /// Gmt time,Ask,Bid,AskVolume,BidVolume
        /// 31.07.2011 23:00:00.652,1.13729,1.13699,2.25,1.13
        /// 31.07.2011 23:00:00.885,1.13733,1.1370200000000001,2.25,1.13
        /// </summary>
        public static TickPrice ParseTickLine(string line)
        {
            if (line[0] == 'G') return null; // skip heading

            var columns = line.Split(',');

            var time = DateTime.ParseExact(columns[0], "dd.MM.yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var ask = decimal.Parse(columns[1], CultureInfo.InvariantCulture);
            //var bid = decimal.Parse(columns[2], CultureInfo.InvariantCulture);

            return new TickPrice(new Instrument("", ""), time, ask /*, bid*/);
        }
    }
}
