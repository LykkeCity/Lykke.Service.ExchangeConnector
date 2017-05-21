using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Infrastructure;
using TradingBot.Trading;

namespace TradingBot.AlphaEngine
{
    /// <summary>
    /// Produce IntrinsicTimeEvents depending on directional change threshold
    /// </summary>
    public class IntrinsicTime
    {
        private ILogger logger = Logging.CreateLogger<IntrinsicTime>();

        public IntrinsicTime(decimal directionalChangeTrheshold)
        {
            upwardDirectionalChangeOriginalThreshold = directionalChangeTrheshold;
            downwardDirectionalChangeOriginalThreshold = directionalChangeTrheshold;

            AdjustThresholds(0);
        }

        public decimal UpwardDirectionalChangeThreshold => upwardDirectionalChangeThreshold;

        public decimal DownwardDirectionalChangeThreshold => downwardDirectionalChangeThreshold;

        private List<IntrinsicTimeEvent> intrinsicTimeEvents = new List<IntrinsicTimeEvent>();

        public IReadOnlyList<IntrinsicTimeEvent> IntrinsicTimeEvents => intrinsicTimeEvents;
        
        private decimal extremPrice;
        private AlgorithmMode mode = AlgorithmMode.Up;

        public AlgorithmMode Mode => mode;
        
        private decimal probabilityIndicator = 1m;

        private decimal defaultCascadingUnits = 1m;
        private decimal defaultDecascadingUnits = 1m;

        
        private decimal upwardDirectionalChangeThreshold;
        private decimal downwardDirectionalChangeThreshold;

        private decimal upwardDirectionalChangeOriginalThreshold;
        private decimal downwardDirectionalChangeOriginalThreshold;

        private decimal upwardOvershootThreshold => upwardDirectionalChangeThreshold * overshootMultiplier;
        private decimal downwardOvershootThreshold => downwardDirectionalChangeThreshold * overshootMultiplier;

        private const decimal overshootMultiplier = 2.525729m;


        private decimal cascadingUnits => defaultCascadingUnits * upwardDirectionalChangeThreshold / downwardDirectionalChangeThreshold;
        

        private decimal LastEventPrice => intrinsicTimeEvents.LastOrDefault()?.Price ?? 0;

        private decimal LastDirectionalChangePrice => 
            intrinsicTimeEvents.OfType<DirectionalChange>().LastOrDefault()?.Price ?? 0;
        
        public IntrinsicTimeEvent OnPriceChange(PriceTime priceTime)
        {
            decimal price = priceTime.Price;
            DateTime time = priceTime.Time;

            IntrinsicTimeEvent result = null;

            decimal priceChangeFromExtrem = CalcPriceChange(price, extremPrice);
            decimal priceChangeFromLastEvent = CalcPriceChange(price, LastEventPrice);

            if (mode == AlgorithmMode.Up)
            {
                if (price > extremPrice)
                {
                    extremPrice = price;
                    
                    if (priceChangeFromLastEvent >= upwardOvershootThreshold)
                    {
                        result = new Overshoot(time, AlgorithmMode.Up, price, price - LastEventPrice, cascadingUnits);
                        AddNewEvent(result);
                    }
                }
                else if (priceChangeFromExtrem <= -downwardDirectionalChangeThreshold)
                {
                    result = new DirectionalChange(time, AlgorithmMode.Down, price, price - extremPrice, cascadingUnits);
                    AddNewEvent(result);

                    extremPrice = price;
                    mode = AlgorithmMode.Down;

                    logger.LogInformation($"DirectionalChange to DOWN event registered");
                }
            }
            else if (mode == AlgorithmMode.Down)
            {
                if (price < extremPrice)
                {
                    extremPrice = price;

                    if (priceChangeFromLastEvent <= -downwardOvershootThreshold)
                    {
                        result = new Overshoot(time, AlgorithmMode.Down, price, price - LastEventPrice, cascadingUnits);
                        AddNewEvent(result);
                    }
                }
                else if (priceChangeFromExtrem >= upwardDirectionalChangeThreshold)
                {
                    result = new DirectionalChange(time, AlgorithmMode.Up, price, price - extremPrice, cascadingUnits);
                    AddNewEvent(result);

                    extremPrice = price;
                    mode = AlgorithmMode.Up;
                    
                    logger.LogInformation($"DirectionalChange to UP event registered");
                }
            }

            return result;
        }

        private decimal CalcPriceChange(decimal currentPrice, decimal basePrice)
        {
            if (basePrice == 0)
                return 0;

            return currentPrice / basePrice - 1;
        }

        private void AddNewEvent(IntrinsicTimeEvent intrinsicTimeEvent)
        {
            intrinsicTimeEvents.Add(intrinsicTimeEvent);
        }

        public Tuple<decimal, decimal> GetAvaregesForDcAndFollowedOs()
        {
            var dcMagnitudes = new List<decimal>(intrinsicTimeEvents.Count);
            var osMagnitudes = new List<decimal>(intrinsicTimeEvents.Count);

            for (int i = 0; i < intrinsicTimeEvents.Count - 1; i++)
            {
                var currentEvent = intrinsicTimeEvents[i];
                var nextEvent = intrinsicTimeEvents[i + 1];

                if (currentEvent is DirectionalChange && nextEvent is Overshoot)
                {
                    dcMagnitudes.Add(Math.Abs(currentEvent.PriceMove));
                    osMagnitudes.Add(Math.Abs(nextEvent.PriceMove));
                    i++;
                }
            }

            if (!dcMagnitudes.Any())
                return Tuple.Create(0m, 0m);

            return Tuple.Create(dcMagnitudes.Sum() / dcMagnitudes.Count, osMagnitudes.Sum() / osMagnitudes.Count);
        }

        private decimal GetDefaultCascadingSize(decimal probabilityIndicator)
        {
            if (probabilityIndicator < 0.1m)
                return 0.1m;

            if (probabilityIndicator < 0.5m)
                return 0.5m;

            return 1m;
        }

        public void AdjustThresholds(decimal longPositionInUnits)
        {
            if (longPositionInUnits >= 30m)
            {
                upwardDirectionalChangeThreshold = 2m * upwardDirectionalChangeOriginalThreshold;
                downwardDirectionalChangeThreshold = 0.5m * downwardDirectionalChangeOriginalThreshold;
            }
            else if (longPositionInUnits >= 15m)
            {
                upwardDirectionalChangeThreshold = 1.5m * upwardDirectionalChangeOriginalThreshold;
                downwardDirectionalChangeThreshold = 0.75m * downwardDirectionalChangeOriginalThreshold;
            }
            else
            {
                upwardDirectionalChangeThreshold = upwardDirectionalChangeOriginalThreshold;
                downwardDirectionalChangeThreshold = downwardDirectionalChangeOriginalThreshold;
            }
        }
    }
}
