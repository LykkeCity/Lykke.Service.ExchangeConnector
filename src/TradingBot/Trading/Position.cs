using System.Collections.Generic;

namespace TradingBot.Trading
{
    public class Position
    {
        public Instrument Instrument { get; }

        public Position(Instrument instrument)
        {
            Instrument = instrument;
        }

        private decimal ProfitLoss;

        private decimal UnrealizedProfitLoss;
        
        public PositionSide Long { get; }
        
        public PositionSide Short { get; }


        public decimal Count => count;

        private decimal count;

        private decimal money;

        public decimal Money => money;

        public decimal Average => money / count;
        
        private List<TradingSignal> signals = new List<TradingSignal>();

        public void AddSignal(TradingSignal signal)
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

        public Position AddAnother(Position another)
        {
            return new Position(Instrument)
            {
                count = count + another.count,
                money = money + another.money
            };
        }
    }
}
