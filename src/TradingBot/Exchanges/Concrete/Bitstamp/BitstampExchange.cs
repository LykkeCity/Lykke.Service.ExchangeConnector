using System;
using System.Threading;
using System.Threading.Tasks;
using PusherClient.DotNetCore;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Bitstamp
{
    public class BitstampExchange : Exchange
    {
        public new static string Name = "bitstamp";
        private readonly BitstampConfiguration config;

        private Pusher pusher;
        
        public BitstampExchange(BitstampConfiguration config, TranslatedSignalsRepository translatedSignalsRepository) 
            : base(Name, config, translatedSignalsRepository)
        {
            this.config = config;
            
            pusher = new Pusher(config.ApplicationKey);
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            pusher.Connected += o => tcs.SetResult(true);
            pusher.Error += (o, e) => tcs.SetResult(false);
            
            pusher.Connect();

            return tcs.Task;
        }

        public override Task OpenPricesStream()
        {
            var channel = pusher.Subscribe("live_trades"); // for BTC/USD
            
            channel.Bind("trade", o => System.Console.WriteLine(o));
            // TODO

            return Task.FromResult(0);

            //throw new NotImplementedException();
        }

        public override Task ClosePricesStream()
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