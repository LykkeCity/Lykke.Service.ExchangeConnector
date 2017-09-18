using System;
using System.Threading.Tasks;
using Common.Log;
using PusherClient.DotNetCore;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Bitstamp
{
    public class BitstampExchange : Exchange
    {
        public new static string Name = "bitstamp";
        private readonly BitstampConfiguration config;
        private readonly ILog log;
        private Pusher pusher;
        
        public BitstampExchange(
            BitstampConfiguration config,
            TranslatedSignalsRepository translatedSignalsRepository,
            ILog log) 
            : base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;
            this.log = log;
        }

        protected override void StartImpl()
        {
            pusher = new Pusher(config.ApplicationKey, log);
            
            pusher.Connected += x =>
            {
                OnConnected();
                var channel = pusher.Subscribe("live_trades"); // for BTC/USD
            
                channel.Bind("trade", o => System.Console.WriteLine(o));    
            };
            
            pusher.Error += (o, e) => OnStopped();
            
            pusher.Connect();
            
            
            // TODO

        }

        protected override void StopImpl()
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}