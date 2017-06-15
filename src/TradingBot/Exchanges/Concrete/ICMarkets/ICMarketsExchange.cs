using System;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.ICMarkets
{
    public class ICMarketsExchange : Exchange
    {
        public ICMarketsExchange() : base("ICMarkets")
        {
        }

        public override void ClosePricesStream()
        {
            throw new NotImplementedException();
        }

        public override Task OpenPricesStream(Instrument[] instruments, Action<InstrumentTickPrices> callback)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
