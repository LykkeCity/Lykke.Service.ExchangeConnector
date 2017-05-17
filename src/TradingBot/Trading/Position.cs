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

        public decimal Count => count;

        private decimal count;

        private decimal money;

        public decimal Money => money;
        
        private List<Signal> signals = new List<Signal>();

        public void AddSignal(Signal signal)
        {
            signals.Add(signal);

            if (signal.Type == SignalType.Long)
            {
                count += signal.Count;
                money -= signal.Amount;
            }
            else if (signal.Type == SignalType.Short)
            {
                count -= signal.Count;
                money += signal.Amount;
            }
        }
    }
}
