using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Infrastructure;

namespace TradingBot.AlphaEngine
{
    public class InstrumentAgent
    {
        private ILogger logger = Logging.CreateLogger<InstrumentAgent>();

        public InstrumentAgent(string instrument, 
            decimal directionalChangeTrheshold)
        {
            Instrument = instrument;
            upwardDirectionalChangeOriginalThreshold = directionalChangeTrheshold;
            downwardDirectionalChangeOriginalThreshold = directionalChangeTrheshold;

            AdjustThresholds();
        }

        public InstrumentAgent(string instrument,
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

        public long DirectionalChangesToUp => intrinsicTimeEvents.OfType<DirectionalChange>().Count(x => x.Mode == AlgorithmMode.Up);

        public long DirectionalChangesToDown => intrinsicTimeEvents.OfType<DirectionalChange>().Count(x => x.Mode == AlgorithmMode.Down);

        private decimal extremPrice;
        private AlgorithmMode mode;
        private decimal dcPrice;
        private decimal cascadingSizeInUnits = 1m;
        private decimal probabilityIndicator = 1m;
        private decimal cumulativeLongPositionInUnits = 0m;
        private decimal cumulativeShortPositionInUnits = 0m;

        private decimal upwardDirectionalChangeThreshold;
        private decimal downwardDirectionalChangeThreshold;
        private decimal upwardDirectionalChangeOriginalThreshold;
        private decimal downwardDirectionalChangeOriginalThreshold;


        public void HandlePriceChange(decimal price, DateTime time)
        {
            decimal priceChange = extremPrice == 0 ? 0 : price / extremPrice - 1;

            if (mode == AlgorithmMode.Up)
            {
                if (price > extremPrice)
                {
                    extremPrice = price;
                }
                else if (priceChange <= -downwardDirectionalChangeThreshold)
                {
                    if (extremPrice > dcPrice)
                    {
                        intrinsicTimeEvents.Add(
                            new Overshoot(time, AlgorithmMode.Up, extremPrice - dcPrice));

                        logger.LogInformation($"{Instrument}: Overwhoot to UP event registered");
                    }

                    intrinsicTimeEvents.Add(
                        new DirectionalChange(time, AlgorithmMode.Down, price - extremPrice));
                    extremPrice = price;
                    dcPrice = price;
                    mode = AlgorithmMode.Down;

                    logger.LogInformation($"{Instrument}: DirectionalChange to DOWN event registered");
                }
            }
            else if (mode == AlgorithmMode.Down)
            {
                if (price < extremPrice)
                {
                    extremPrice = price;
                }
                else if (priceChange >= upwardDirectionalChangeThreshold)
                {
                    if (extremPrice < dcPrice)
                    {
                        intrinsicTimeEvents.Add(
                            new Overshoot(time, AlgorithmMode.Down, dcPrice - extremPrice));

                        logger.LogInformation($"{Instrument}: Overwhoot to DOWN event registered");
                    }

                    intrinsicTimeEvents.Add(
                        new DirectionalChange(time, AlgorithmMode.Up, price - extremPrice));
                    extremPrice = price;
                    dcPrice = price;
                    mode = AlgorithmMode.Up;
                    
                    logger.LogInformation($"{Instrument}: DirectionalChange to UP event registered");
                }
            }
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
