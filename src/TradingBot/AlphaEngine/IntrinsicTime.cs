using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Infrastructure;

namespace TradingBot.AlphaEngine
{
    /// <summary>
    /// Produce IntrinsicTimeEvents depending on directional change threshold
    /// </summary>
    public class IntrinsicTime
    {
        private ILogger logger = Logging.CreateLogger<IntrinsicTime>();

        public IntrinsicTime(string instrument, 
            decimal directionalChangeTrheshold)
        {
            Instrument = instrument;
            upwardDirectionalChangeOriginalThreshold = directionalChangeTrheshold;
            downwardDirectionalChangeOriginalThreshold = directionalChangeTrheshold;

            AdjustThresholds();
        }

        public IntrinsicTime(string instrument,
            decimal upDcTrheshold, decimal downDcThreshold)
        {
            Instrument = instrument;
            upwardDirectionalChangeOriginalThreshold = upDcTrheshold;
            downwardDirectionalChangeOriginalThreshold = downDcThreshold;

            AdjustThresholds();
        }
        
        public string Instrument { get; }

        public decimal UpwardDirectionalChangeThreshold => upwardDirectionalChangeThreshold;

        public decimal DownwardDirectionalChangeThreshold => downwardDirectionalChangeThreshold;

        private List<IntrinsicTimeEvent> intrinsicTimeEvents = new List<IntrinsicTimeEvent>();

        public IReadOnlyList<IntrinsicTimeEvent> IntrinsicTimeEvents => intrinsicTimeEvents;


        public long UpwardDirectionalChangesCount => 
            intrinsicTimeEvents.OfType<DirectionalChange>().Count(x => x.Mode == AlgorithmMode.Up);

        public long DownwardDirectionalChangesCount => 
            intrinsicTimeEvents.OfType<DirectionalChange>().Count(x => x.Mode == AlgorithmMode.Down);

        public long UpwardOvershootsCount => 
            intrinsicTimeEvents.OfType<Overshoot>().Count(x => x.Mode == AlgorithmMode.Up);

        public long DownwardOvershootsCount =>
            intrinsicTimeEvents.OfType<Overshoot>().Count(x => x.Mode == AlgorithmMode.Down);


        public event Action<IntrinsicTimeEvent> NewIntrinsicTimeEventGenerated;


        private decimal extremPrice;
        private AlgorithmMode mode;


        private decimal cascadingSizeInUnits = 1m;
        private decimal probabilityIndicator = 1m;
        private decimal cumulativeLongPositionInUnits = 0m; // todo: use type Trading.Position
        private decimal cumulativeShortPositionInUnits = 0m;
        
        private decimal upwardDirectionalChangeThreshold;
        private decimal downwardDirectionalChangeThreshold;
        private decimal upwardDirectionalChangeOriginalThreshold;
        private decimal downwardDirectionalChangeOriginalThreshold;

        private decimal LastEventPrice => intrinsicTimeEvents.LastOrDefault()?.Price ?? 0;

        private decimal LastDirectionalChangePrice => intrinsicTimeEvents.OfType<DirectionalChange>().LastOrDefault()?.Price ?? 0;

        public DirectionalChange LastDirectionalChange => intrinsicTimeEvents.OfType<DirectionalChange>().LastOrDefault();

        public void HandlePriceChange(decimal price, DateTime time)
        {
            decimal priceChangeFromExtrem = CalcPriceChange(price, extremPrice);
            decimal priceChangeFromLastEvent = CalcPriceChange(price, LastEventPrice);

            if (mode == AlgorithmMode.Up)
            {
                if (price > extremPrice)
                {
                    extremPrice = price;
                    
                    if (priceChangeFromLastEvent >= upwardDirectionalChangeThreshold)
                    {
                        AddNewEvent(new Overshoot(time, AlgorithmMode.Up, price, price - LastEventPrice));
                    }
                }
                else if (priceChangeFromExtrem <= -downwardDirectionalChangeThreshold)
                {
                    if (LastDirectionalChangePrice != 0 && extremPrice > LastDirectionalChangePrice)
                    {
                        AddNewEvent(new Overshoot(time, AlgorithmMode.Up, price, extremPrice - LastDirectionalChangePrice));

                        logger.LogInformation($"{Instrument}: Overshoot to UP event registered");
                    }

                    AddNewEvent(new DirectionalChange(time, AlgorithmMode.Down, price, price - extremPrice));

                    extremPrice = price;
                    mode = AlgorithmMode.Down;

                    logger.LogInformation($"{Instrument}: DirectionalChange to DOWN event registered");
                }
            }
            else if (mode == AlgorithmMode.Down)
            {
                if (price < extremPrice)
                {
                    extremPrice = price;

                    if (priceChangeFromLastEvent <= -downwardDirectionalChangeThreshold)
                    {
                        AddNewEvent(new Overshoot(time, AlgorithmMode.Down, price, price - LastEventPrice));
                    }
                }
                else if (priceChangeFromExtrem >= upwardDirectionalChangeThreshold)
                {
                    if (LastDirectionalChangePrice != 0 && extremPrice < LastDirectionalChangePrice)
                    {
                        AddNewEvent(new Overshoot(time, AlgorithmMode.Down, price, LastDirectionalChangePrice - extremPrice));

                        logger.LogInformation($"{Instrument}: Overwhoot to DOWN event registered");
                    }

                    AddNewEvent(new DirectionalChange(time, AlgorithmMode.Up, price, price - extremPrice));
                    extremPrice = price;
                    mode = AlgorithmMode.Up;
                    
                    logger.LogInformation($"{Instrument}: DirectionalChange to UP event registered");
                }
            }
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

            NewIntrinsicTimeEventGenerated?.Invoke(intrinsicTimeEvent);
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

        private void AddLongPosition(decimal sumInUnits)
        {
            cumulativeLongPositionInUnits += sumInUnits;

            AdjustThresholds();
        }

        private void AdjustThresholds()
        {
            if (cumulativeLongPositionInUnits >= 30m)
            {
                upwardDirectionalChangeThreshold = 2m * upwardDirectionalChangeOriginalThreshold;
                downwardDirectionalChangeThreshold = 0.5m * downwardDirectionalChangeOriginalThreshold;
            }
            else if (cumulativeLongPositionInUnits >= 15m)
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
