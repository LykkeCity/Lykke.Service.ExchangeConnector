namespace TradingBot.Trading
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
