using System;
using System.Collections.Generic;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
{
    public class AlphaEngineAgent : ITradingAgent
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

        private readonly List<CoastlineTrader> coastlineTraders;

        private IntrinsicNetwork intrinsicNetwork;

        //private byte[] intrinsicNetworkState = new byte[IntrinsicNetworkDimensions];

        public void OnPriceChange(TickPrice priceTime)
        {
            foreach (var ct in coastlineTraders)
            {
                var signal = ct.OnPriceChange(priceTime);
                
                if (signal != null)
                {
                    TradingSignalGenerated?.Invoke(signal);    
                }
            }
        }

        public void OnPriceChange(TickPrice[] tickPrices)
        {
            foreach (var tickPrice in tickPrices)
            {
                OnPriceChange(tickPrice);
            }
        }

        public event Action<TradingSignal> TradingSignalGenerated;

        public Position GetCumulativePosition()
        {
            var position = new Position(Instrument);
            coastlineTraders.ForEach(x => position = position.AddAnother(x.Position));
            return position;
        }
    }
}
