using System.Collections;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class ExchangesConfiguration : IEnumerable<IExchangeConfiguration>
    {
        public IcmExchangeConfiguration Icm { get; set; }

        public KrakenConfig Kraken { get; set; }

        public StubExchangeConfiguration Stub { get; set; }

        public HistoricalDataConfig HistoricalData { get; set; }

        public LykkeExchangeConfiguration Lykke { get; set; }

        public BitMexExchangeConfiguration BitMex { get; set; }

        public BitfinexExchangeConfiguration Bitfinex { get; set; }

        public GdaxExchangeConfiguration Gdax { get; set; }

        public JfdExchangeConfiguration Jfd { get; set; }

        public IEnumerator<IExchangeConfiguration> GetEnumerator()
        {
            yield return Icm;
            yield return Kraken;
            yield return Stub;
            yield return HistoricalData;
            yield return Lykke;
            yield return BitMex;
            yield return Bitfinex;
            yield return Gdax;
            yield return Jfd;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
