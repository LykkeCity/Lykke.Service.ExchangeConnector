using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.HistoricalData
{
    internal class HistoricalDataExchange : Exchange
    {
        public new static readonly string Name = "historical";
        
        private readonly HistoricalDataConfig config;
        private readonly IHandler<TickPrice> _tickHandler;

        private HistoricalDataReader reader;
        private bool stopRequested;
        
        public HistoricalDataExchange(HistoricalDataConfig config, TranslatedSignalsRepository translatedSignalsRepository,IHandler<TickPrice> tickHandler, ILog log) : 
            base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;
            _tickHandler = tickHandler;
        }

        private Task pricesCycle;

        protected override void StartImpl()
        {
            stopRequested = false;

            pricesCycle = Task.Run(async () =>
            {
                var paths = new List<string>();

                for (DateTime day = config.StartDate; day <= config.EndDate; day = day.AddDays(1))
                {
                    var fileName = string.Format(config.FileName, day);
                    paths.Add(config.BaseDirectory + fileName);
                }
            
                reader = new HistoricalDataReader(paths.ToArray(), LineParsers.ParseTickLine);
                OnConnected();
            
                using (var enumerator = reader.GetEnumerator())
                    while (!stopRequested && enumerator.MoveNext())
                    {
                        await _tickHandler.Handle(enumerator.Current);
                    }
            });
        }

        protected override void StopImpl()
        {
            stopRequested = true;
            reader?.Dispose();
        }

        public override Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
