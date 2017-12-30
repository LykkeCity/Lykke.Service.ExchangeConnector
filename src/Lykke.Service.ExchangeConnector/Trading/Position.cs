using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Trading
{
    public class Position
    {
        private readonly ILogger _logger = Logging.CreateLogger<Position>();
        
        public Instrument Instrument { get; }

        public Position(Instrument instrument, decimal initialValue)
        {
            Instrument = instrument;
            _initialValue = initialValue;
            _currentValue = initialValue;
            _assetsVolume = 0m;
        }

        /// <summary>
        /// Initial amount of assets (base asset of the Instrument)
        /// </summary>
        private readonly decimal _initialValue; // TODO: position may not have initialValue, and be a part of Portfolio with initial value

        private decimal _currentValue;

        private decimal _assetsVolume;
        
        public decimal AssetsVolume => _assetsVolume;

        public decimal AveragePrice => (_initialValue - _currentValue) / _assetsVolume;
        
        public decimal RealizedPnL => (_currentValue - _initialValue) / _initialValue; // TODO~!!!!

//        private decimal realizedProfit;
        
        private readonly LinkedList<ExecutionReport> _longSide = new LinkedList<ExecutionReport>();
        private readonly LinkedList<ExecutionReport> _shortSide = new LinkedList<ExecutionReport>();

        public decimal GetPnL(decimal price) // TODO: price should be Ask or Bid in dependence of position
        {
            //return (assetsVolume * price + currentValue - initialValue) / initialValue;

            decimal total = 0;
            foreach (var trade in _longSide)
            {
                total += (price - trade.Price) * trade.Volume;
            }

            foreach (var trade in _shortSide)
            {
                total += (trade.Price - price) * trade.Volume;
            }

            return total;
        }

        private decimal BalancedVolume => Math.Min(_longSide.Sum(x => x.Volume), _shortSide.Sum(x => x.Volume));

        
        public void AddTrade(ExecutionReport trade) // todo: make a thread-safe method
        {
            if (trade.Type == TradeType.Buy)
            {
                _longSide.AddLast(trade);
                _logger.LogDebug($"Long side of inventory increased: {trade}. Total Long: {_longSide.Sum(x => x.Volume)}. Total PnL: {GetPnL(trade.Price)}");
            }
            else if (trade.Type == TradeType.Sell)
            {
                _shortSide.AddLast(trade);
                
                _logger.LogDebug($"Short side of inventory increased: {trade}. Total Short: {_shortSide.Sum(x => x.Volume)}. Total PnL: {GetPnL(trade.Price)}");
            }
        }

        

        public Position AddAnother(Position another)
        {
            return new Position(Instrument, _initialValue + another._initialValue)
            {
                _currentValue = _currentValue + another._currentValue,
                _assetsVolume = _assetsVolume + another._assetsVolume
            };
        }
    }
}
