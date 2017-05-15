using System.Collections.Generic;

namespace TradingBot.Trading
{
    public class Position
    {
        public string Instrument { get; }

        public Position(string instrument)
        {
            Instrument = instrument;
        }

        public decimal Amount => amount;

        private decimal amount;
        
        private List<Signal> signals = new List<Signal>();

        public void AddSignal(Signal signal)
        {
            signals.Add(signal);

            if (signal.Type == SignalType.Long)
            {
                amount += signal.Amount;
            }
            else if (signal.Type == SignalType.Short)
            {
                amount -= signal.Amount;
            }
        }
    }
}
