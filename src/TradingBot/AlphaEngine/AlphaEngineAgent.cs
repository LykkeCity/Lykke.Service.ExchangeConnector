using System;
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
                new CoastlineTrader(instrument, new IntrinsicTime(0.00025m)),
                new CoastlineTrader(instrument, new IntrinsicTime(0.0005m)),
                new CoastlineTrader(instrument, new IntrinsicTime(0.001m)),
                new CoastlineTrader(instrument, new IntrinsicTime(0.0015m))
            };

            intrinsicNetwork = new IntrinsicNetwork(IntrinsicNetworkDimensions, 
                firstThreshold: 0.00025m, 
                liquiditySlidingWindow: TimeSpan.FromMinutes(10));
        }

        private const int IntrinsicNetworkDimensions = 12;

        public Instrument Instrument { get; set; }

        private List<CoastlineTrader> coastlineTraders;

        private IntrinsicNetwork intrinsicNetwork;

        //private byte[] intrinsicNetworkState = new byte[IntrinsicNetworkDimensions];

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
