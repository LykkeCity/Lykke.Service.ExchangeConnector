using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.HistoricalData
{
    public class HistoricalDataExchange : Exchange
    {
        private readonly HistoricalDataConfig config;

        private HistoricalDataReader reader;
        private bool stopRequested;
        
        public HistoricalDataExchange(HistoricalDataConfig config) : base("HistoricalData", config)
        {
            this.config = config;
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            return Task.FromResult(File.Exists(config.BaseDirectory + string.Format(config.FileName, config.StartDate)));
        }

        public override Task OpenPricesStream(Action<InstrumentTickPrices> callback)
        {
            stopRequested = false;
            var paths = new List<string>();

            for (DateTime day = config.StartDate; day <= config.EndDate; day = day.AddDays(1))
            {
                var fileName = string.Format(config.FileName, day);
                paths.Add(config.BaseDirectory + fileName);
            }
            
            reader = new HistoricalDataReader(paths.ToArray(), LineParsers.ParseTickLine);
            
            using (var enumerator = reader.GetEnumerator())
                while (!stopRequested && enumerator.MoveNext())
                {
                    callback(new InstrumentTickPrices(Instruments.First(), new TickPrice[] { enumerator.Current }));
                }
            
            return Task.FromResult(0);
        }

        public override void ClosePricesStream()
        {
            stopRequested = true;
            reader?.Dispose();
        }
    }
}