using Newtonsoft.Json;

namespace TradingBot.Trading
{
    public class InstrumentTradingSignals
    {
        public Instrument Instrument { get; }
        
        public TradingSignal[] TradingSignals { get; }

        [JsonConstructor]
        public InstrumentTradingSignals(Instrument instrument, TradingSignal[] tradingSignals)
        {
            Instrument = instrument;
            TradingSignals = tradingSignals;
        }
    }
}