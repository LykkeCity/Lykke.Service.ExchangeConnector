using System.Collections.Generic;

namespace TradingBot.Common.Trading
{
    public class Position
    {
        public Instrument Instrument { get; }

        public Position(Instrument instrument, decimal initialValue)
        {
            Instrument = instrument;
            this.initialValue = initialValue;
            this.currentValue = initialValue;
            this.assetsVolume = 0m;
        }

        /// <summary>
        /// Initial amount of assets (base asset of the Instrument)
        /// </summary>
        private readonly decimal initialValue;

        private decimal currentValue;

        private decimal assetsVolume;
        
        public decimal AssetsVolume => assetsVolume;

        public decimal AveragePrice => (initialValue - currentValue) / assetsVolume;
        
        public decimal RealizedPnL => (currentValue - initialValue) / initialValue;
        
        private readonly List<ExecutedTrade> trades = new List<ExecutedTrade>();
        
        public void AddTrade(ExecutedTrade trade)
        {
            //trades.Add(trade);

            if (trade.Type == TradeType.Buy)
            {
                currentValue -= trade.Price * trade.Volume;
                assetsVolume += trade.Volume;
            }
            else if (trade.Type == TradeType.Sell)
            {
                currentValue += trade.Price * trade.Volume;
                assetsVolume -= trade.Volume;
            }
        }

        public Position AddAnother(Position another)
        {
            return new Position(Instrument, initialValue + another.initialValue)
            {
                currentValue = currentValue + another.currentValue,
                assetsVolume = assetsVolume + another.assetsVolume
            };
        }
    }
}
