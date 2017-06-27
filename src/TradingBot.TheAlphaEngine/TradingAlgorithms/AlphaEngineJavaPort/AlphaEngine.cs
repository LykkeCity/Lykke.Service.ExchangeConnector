using System;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngineJavaPort
{
    public class AlphaEngine : ITradingAgent
    {
        public AlphaEngine(string pair)
        {
			double[] deltas = { 0.25 / 100.0, 0.5 / 100.0, 1.0 / 100.0, 1.5 / 100.0 };

			fxRateTrading = new FxRateTrading(pair, deltas.Length, deltas);
            this.pair = pair;
        }

        private readonly FxRateTrading fxRateTrading;
        private string pair;

        public void OnPriceChange(TickPrice[] prices)
        {
            foreach (var price in prices)
            {
                OnPriceChange(price);
            }
        }

        public void OnPriceChange(TickPrice tickPrice)
        {
            fxRateTrading.RunTradingAsymm(tickPrice);
        }

        public event Action<TradingSignal> TradingSignalGenerated;
    }
}
