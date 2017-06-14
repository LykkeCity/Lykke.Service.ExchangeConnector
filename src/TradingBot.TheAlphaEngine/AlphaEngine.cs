using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine
{
    public class AlphaEngine
    {
        public AlphaEngine(string pair)
        {
			double[] deltas = { 0.25 / 100.0, 0.5 / 100.0, 1.0 / 100.0, 1.5 / 100.0 };

			fxRateTrading = new FxRateTrading(pair, deltas.Length, deltas);
            this.pair = pair;
        }

        private FxRateTrading fxRateTrading;
        private string pair;

        public void OnPriceChanged(TickPrice[] prices)
        {
            foreach (var price in prices)
            {
                fxRateTrading.RunTradingAsymm(price);
            }
        }
    }
}
