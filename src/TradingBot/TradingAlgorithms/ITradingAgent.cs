using System;
using TradingBot.Common.Trading;
using TradingBot.Trading;

namespace TradingBot.TradingAlgorithms
{
    public interface ITradingAgent
    {
        void OnPriceChange(TickPrice tickPrice);

        event Action<TradingSignal> TradingSignalGenerated;
    }
}