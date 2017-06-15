using System;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    public class StubExchange : Exchange
    {
        public StubExchange() : base("Stub Exchange Implementation")
        {
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
		}


        private bool closePricesStreamRequested;
        private Random random;

		public override async Task OpenPricesStream(Instrument[] instruments, Action<InstrumentTickPrices> callback)
		{
            closePricesStreamRequested = false;
            random = new Random();

            decimal price = 100m;

            while (!closePricesStreamRequested)
            {
                bool grow = random.NextDouble() >= 0.5;
                decimal percents = (decimal) random.NextDouble() / 100;
                decimal delta = price * percents;

                if (grow)
                    price += delta;
                else
                    price -= delta;

                callback(new InstrumentTickPrices(instruments[0], new[] { new TickPrice(DateTime.Now, price) }));

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
		}


        public override void ClosePricesStream()
        {
            closePricesStreamRequested = true;
        }
    }
}
