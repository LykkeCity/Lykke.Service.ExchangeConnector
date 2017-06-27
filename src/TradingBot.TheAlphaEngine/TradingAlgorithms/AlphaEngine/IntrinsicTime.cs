using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
{
    /// <summary>
    /// Produce IntrinsicTimeEvents depending on directional change threshold
    /// </summary>
    public class IntrinsicTime
    {
        private ILogger logger = Logging.CreateLogger<IntrinsicTime>();

        public IntrinsicTime(decimal directionalChangeThreshold)
        {
            upwardDirectionalChangeOriginalThreshold = directionalChangeThreshold;
            downwardDirectionalChangeOriginalThreshold = directionalChangeThreshold;

            AdjustThresholds(0);
        }
        
        private List<IntrinsicTimeEvent> intrinsicTimeEvents = new List<IntrinsicTimeEvent>();

        public IReadOnlyList<IntrinsicTimeEvent> IntrinsicTimeEvents => intrinsicTimeEvents;
        
        private decimal extremPrice;
        private AlgorithmMode mode = AlgorithmMode.Up;

        public AlgorithmMode Mode => mode;
        
        
        private decimal defaultCascadingUnits = 1m;
        private decimal defaultDecascadingUnits = 1m;

        
        private decimal upwardDirectionalChangeThreshold;
        private decimal downwardDirectionalChangeThreshold;

        private decimal upwardDirectionalChangeOriginalThreshold;
        private decimal downwardDirectionalChangeOriginalThreshold;

        private decimal upwardOvershootThreshold => upwardDirectionalChangeThreshold * overshootMultiplier;
        private decimal downwardOvershootThreshold => downwardDirectionalChangeThreshold * overshootMultiplier;

        private const decimal overshootMultiplier = 1;//2.525729m;


        private decimal cascadingUnits => defaultCascadingUnits * upwardDirectionalChangeThreshold / downwardDirectionalChangeThreshold;
        

        private decimal LastEventPrice => intrinsicTimeEvents.LastOrDefault()?.Price ?? extremPrice;
        
        public IntrinsicTimeEvent OnPriceChange(TickPrice tickPrice)
        {
            decimal price = tickPrice.Mid;
            DateTime time = tickPrice.Time;

            IntrinsicTimeEvent result = null;

            if (extremPrice == 0)
            {
                extremPrice = price;
                return result;
            }

            decimal priceChangeFromExtrem = price / extremPrice - 1m; //CalcPriceChange(price, extremPrice);
            decimal priceChangeFromLastEvent = price / LastEventPrice - 1m; //CalcPriceChange(price, LastEventPrice);

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

                    //logger.LogInformation($"DirectionalChange to DOWN event registered");
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

                    //logger.LogInformation($"DirectionalChange to UP event registered");
                }
            }

            return result;
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
