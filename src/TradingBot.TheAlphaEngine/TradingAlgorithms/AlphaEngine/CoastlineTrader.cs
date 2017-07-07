using System.Collections.Generic;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
{
    public class CoastlineTrader
    {
        public CoastlineTrader(Instrument instrument, IntrinsicTime intrinsicTime, decimal initialValue)
        {
            Instrument = instrument;

            Position = new Position(instrument, initialValue);
            IntrinsicTime = intrinsicTime;
        }

        public Instrument Instrument { get; }

        private decimal unitSize = 1m;

        public Position Position { get; }

        public IntrinsicTime IntrinsicTime { get; }

        public TradingSignal OnPriceChange(TickPrice priceTime)
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
                result = new TradingSignal(TradeType.Buy, intrinsicTimeEvent.Price, intrinsicTimeEvent.CascadingUnits * unitSize, intrinsicTimeEvent.Time);
                // it's required to get units from engine agent
            }
            else
            {
                result = new TradingSignal(TradeType.Sell, intrinsicTimeEvent.Price, intrinsicTimeEvent.CascadingUnits * unitSize, intrinsicTimeEvent.Time);
            }

            AddNewSignal(result);

            IntrinsicTime.AdjustThresholds(Position.AssetsVolume);

            return result;
        }

        private void AddNewSignal(TradingSignal signal)
        {
            signals.Add(signal);
            //Position.AddSignal(signal);
        }

        private List<TradingSignal> signals = new List<TradingSignal>();

    }
}
