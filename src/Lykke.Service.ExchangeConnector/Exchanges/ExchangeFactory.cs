using System.Collections.Generic;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot.Exchanges
{
    internal sealed class ExchangeFactory
    {
        private readonly IReadOnlyCollection<Exchange> _implementations;

        public ExchangeFactory(IReadOnlyCollection<Exchange> implementations)
        {
            _implementations = implementations;
        }

        public IReadOnlyCollection<Exchange> CreateExchanges()
        {
            return _implementations;
        }
    }
}
