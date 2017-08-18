using System.Collections;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class ExchangesConfiguration : IEnumerable<IExchangeConfiguration>
    {
        public IcmConfig Icm { get; set; }
        
        public KrakenConfig Kraken { get; set; }
        
        public StubExchangeConfiguration Stub { get; set; }
        
        public HistoricalDataConfig HistoricalData { get; set; }
        
        
        public IEnumerator<IExchangeConfiguration> GetEnumerator()
        {
            yield return Icm;
            yield return Kraken;
            yield return Stub;
            yield return HistoricalData;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
