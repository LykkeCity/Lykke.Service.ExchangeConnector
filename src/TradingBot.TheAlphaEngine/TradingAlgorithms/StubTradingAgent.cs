using System;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms
{
    public class StubTradingAgent : ITradingAgent
    {
        public void OnPriceChange(TickPrice tickPrice)
        {
            TradingSignalGenerated?.Invoke(CreateSignal(tickPrice));
        }

        public void OnPriceChange(TickPrice[] prices)
        {
            foreach (var price in prices)
            {
                OnPriceChange(price);
            }
        }

        public event Action<TradingSignal> TradingSignalGenerated;

        private decimal lastPrice;
        
        private TradingSignal CreateSignal(TickPrice price)
        {
            var signalType = price.Ask > lastPrice ? SignalType.Long : SignalType.Short;
            lastPrice = price.Ask;
            
            var signal = new TradingSignal(signalType, price.Ask, 1, price.Time);

            return signal;
        }
    }
}