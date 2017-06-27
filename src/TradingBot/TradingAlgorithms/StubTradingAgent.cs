using System;
using TradingBot.Common.Trading;
using TradingBot.Trading;

namespace TradingBot.TradingAlgorithms
{
    public class StubTradingAgent : ITradingAgent
    {
        public void OnPriceChange(TickPrice tickPrice)
        {
            TradingSignalGenerated?.Invoke(CreateSignal(tickPrice));
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