using System;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms
{
    public interface ITradingAgent
    {
        void OnPriceChange(TickPrice tickPrice);
        
        void OnPriceChange(TickPrice[] prices);

        event Action<TradingSignal> TradingSignalGenerated;
    }
}