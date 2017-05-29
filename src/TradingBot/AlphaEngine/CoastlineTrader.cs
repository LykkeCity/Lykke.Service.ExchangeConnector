using System.Collections.Generic;
using TradingBot.Trading;

namespace TradingBot.AlphaEngine
{
    public class CoastlineTrader
    {
        public CoastlineTrader(Instrument instrument, IntrinsicTime intrinsicTime)
        {
            Instrument = instrument;

            Position = new Position(instrument);
            IntrinsicTime = intrinsicTime;
        }

        public Instrument Instrument { get; }

        private decimal unitSize = 1m;

        public Position Position { get; }

        public IntrinsicTime IntrinsicTime { get; }

        public TradingSignal OnPriceChange(PriceTime priceTime)
        {
            var intrinsicTimeEvent = IntrinsicTime.OnPriceChange(priceTime);
            return OnIntrinsicTimeEvent(intrinsicTimeEvent);
        }

        public TradingSignal OnIntrinsicTimeEvent(IntrinsicTimeEvent intrinsicTimeEvent)
        {
            TradingSignal result = null;

            if (intrinsicTimeEvent == null)
                return result;
            
            if (intrinsicTimeEvent.Mode == AlgorithmMode.Down)
            {
                result = new TradingSignal(SignalType.Long, intrinsicTimeEvent.Price, intrinsicTimeEvent.CascadingUnits * unitSize, intrinsicTimeEvent.Time);
                // it's required to get units from engine agent
            }
            else
            {
                result = new TradingSignal(SignalType.Short, intrinsicTimeEvent.Price, intrinsicTimeEvent.CascadingUnits * unitSize, intrinsicTimeEvent.Time);
            }

            AddNewSignal(result);

            IntrinsicTime.AdjustThresholds(Position.Count);

            return result;
        }

        private void AddNewSignal(TradingSignal signal)
        {
            signals.Add(signal);
            Position.AddSignal(signal);
        }

        private List<TradingSignal> signals = new List<TradingSignal>();

    }
}
