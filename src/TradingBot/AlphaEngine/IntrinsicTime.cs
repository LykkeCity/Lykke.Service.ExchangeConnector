using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Common.Infrastructure;
using TradingBot.Trading;
using TradingBot.Common.Trading;

namespace TradingBot.AlphaEngine
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
            IntrinsicTimeEvent result = null;
            
            if (extremPrice == 0)
            {
                extremPrice = tickPrice.Mid;
                return result;
            }
            
            if (mode == AlgorithmMode.Up)
            {
                if (tickPrice.Bid > extremPrice)
                {
                    extremPrice = tickPrice.Bid;
                    
                    if (tickPrice.Bid / LastEventPrice - 1m >= upwardOvershootThreshold)
                    {
                        result = new Overshoot(tickPrice.Time, AlgorithmMode.Up, tickPrice.Bid, tickPrice.Bid - LastEventPrice, cascadingUnits);
                        AddNewEvent(result);
                    }
                }
                else if (tickPrice.Ask / extremPrice - 1m <= -downwardDirectionalChangeThreshold)
                {
                    result = new DirectionalChange(tickPrice.Time, AlgorithmMode.Down, tickPrice.Bid, tickPrice.Bid - extremPrice, cascadingUnits);
                    AddNewEvent(result);

                    extremPrice = tickPrice.Bid;
                    mode = AlgorithmMode.Down;

                    //logger.LogInformation($"DirectionalChange to DOWN event registered");
                }
            }
            else if (mode == AlgorithmMode.Down)
            {
                if (tickPrice.Ask < extremPrice)
                {
                    extremPrice = tickPrice.Ask;

                    if (tickPrice.Ask / LastEventPrice - 1m <= -downwardOvershootThreshold)
                    {
                        result = new Overshoot(tickPrice.Time, AlgorithmMode.Down, tickPrice.Ask, tickPrice.Ask - LastEventPrice, cascadingUnits);
                        AddNewEvent(result);
                    }
                }
                else if (tickPrice.Bid / extremPrice - 1m >= upwardDirectionalChangeThreshold)
                {
                    result = new DirectionalChange(tickPrice.Time, AlgorithmMode.Up, tickPrice.Ask, tickPrice.Ask - extremPrice, cascadingUnits);
                    AddNewEvent(result);

                    extremPrice = tickPrice.Ask;
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
