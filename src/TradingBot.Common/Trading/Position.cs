using System;
using System.Collections.Generic;
using System.Linq;
using Common.PasswordTools;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;

namespace TradingBot.Common.Trading
{
    public class Position
    {
        private readonly ILogger logger = Logging.CreateLogger<Position>();
        
        public Instrument Instrument { get; }

        public Position(Instrument instrument, decimal initialValue)
        {
            Instrument = instrument;
            this.initialValue = initialValue;
            this.currentValue = initialValue;
            this.assetsVolume = 0m;
        }

        /// <summary>
        /// Initial amount of assets (base asset of the Instrument)
        /// </summary>
        private readonly decimal initialValue; // TODO: position may not have initialValue, and be a part of Portfolio with initial value

        private decimal currentValue;

        private decimal assetsVolume;
        
        public decimal AssetsVolume => assetsVolume;

        public decimal AveragePrice => (initialValue - currentValue) / assetsVolume;
        
        public decimal RealizedPnL => (currentValue - initialValue) / initialValue; // TODO~!!!!

        private decimal realizedProfit;
        
        private readonly LinkedList<ExecutedTrade> longSide = new LinkedList<ExecutedTrade>();
        private readonly LinkedList<ExecutedTrade> shortSide = new LinkedList<ExecutedTrade>();

        public decimal GetPnL(decimal price) // TODO: price should be Ask or Bid in dependence of position
        {
            //return (assetsVolume * price + currentValue - initialValue) / initialValue;

            decimal total = 0;
            foreach (var trade in longSide)
            {
                total += (price - trade.Price) * trade.Volume;
            }

            foreach (var trade in shortSide)
            {
                total += (trade.Price - price) * trade.Volume;
            }

            return total;
        }

        private decimal BalancedVolume => Math.Min(longSide.Sum(x => x.Volume), shortSide.Sum(x => x.Volume));

        
        public void AddTrade(ExecutedTrade trade) // todo: make a thread-safe method
        {
            if (trade.Type == TradeType.Buy)
            {
                longSide.AddLast(trade);
                logger.LogDebug($"Long side of inventory increased: {trade}. Total Long: {longSide.Sum(x => x.Volume)}. Total PnL: {GetPnL(trade.Price)}");
            }
            else if (trade.Type == TradeType.Sell)
            {
                shortSide.AddLast(trade);
                
                logger.LogDebug($"Short side of inventory increased: {trade}. Total Short: {shortSide.Sum(x => x.Volume)}. Total PnL: {GetPnL(trade.Price)}");
            }
        }

        

        public Position AddAnother(Position another)
        {
            return new Position(Instrument, initialValue + another.initialValue)
            {
                currentValue = currentValue + another.currentValue,
                assetsVolume = assetsVolume + another.assetsVolume
            };
        }
    }
}
