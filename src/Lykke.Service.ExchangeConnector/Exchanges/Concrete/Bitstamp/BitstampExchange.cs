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
    internal class BitstampExchange : Exchange
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


        public override Task<OrderStatusUpdate> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task<OrderStatusUpdate> CancelOrderAndWaitExecution(TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
