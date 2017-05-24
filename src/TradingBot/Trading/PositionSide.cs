using System;
using System.Collections.Generic;
using System.Text;

namespace TradingBot.Trading
{
    public class PositionSide
    {
        public decimal Units { get; }

        public decimal AvaragePrice { get; }

        public decimal ProfitLoss { get; set; }

        public decimal UnrealizedProfitLoss { get; set; }

        public void AddSignal(TradingSignal signal)
        {
        }
    }
}
