using System.Collections.Generic;
using TradingBot.Trading;

namespace TradingBot.AlphaEngine
{
    public class AlphaEngineAgent
    {
        public AlphaEngineAgent(Instrument instrument)
        {
            Instrument = instrument;
            coastlineTraders = new List<CoastlineTrader>
            {
                new CoastlineTrader(instrument, new IntrinsicTime(0.0025m)),
                new CoastlineTrader(instrument, new IntrinsicTime(0.005m)),
                new CoastlineTrader(instrument, new IntrinsicTime(0.01m)),
                new CoastlineTrader(instrument, new IntrinsicTime(0.015m))
            };
        }

        public Instrument Instrument { get; set; }

        private List<CoastlineTrader> coastlineTraders;

        public void OnPriceChange(PriceTime priceTime)
        {
            foreach (var ct in coastlineTraders)
            {
                ct.OnPriceChange(priceTime);
            }
        }

        public Position GetCumulativePosition()
        {
            var position = new Position(Instrument);
            coastlineTraders.ForEach(x => position = position.AddAnother(x.Position));
            return position;
        }
    }
}
