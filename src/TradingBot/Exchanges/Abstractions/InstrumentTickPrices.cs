using TradingBot.Common.Trading;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Abstractions
{
    public class InstrumentTickPrices
    {
        public Instrument Instrument { get; set; }
        public TickPrice[] TickPrices { get; set; }

        public InstrumentTickPrices(Instrument instrument, TickPrice[] tickPrices)
        {
            TickPrices = tickPrices;
            Instrument = instrument;
        }
    }
}
