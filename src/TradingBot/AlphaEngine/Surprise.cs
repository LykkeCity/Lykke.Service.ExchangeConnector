using System;

namespace TradingBot.AlphaEngine
{
    public class Surprise
    {
        private DateTime time;
        private double probabilityIndicator;
        private double surprise;

        private readonly NetworkState previousState;
        private readonly NetworkState currentState;

        public NetworkState PreviousState => previousState;
        public NetworkState CurrentState => currentState;

        public Surprise(DateTime time, 
            NetworkState previousState, 
            NetworkState currentState,
            decimal[] thresholds)
        {
            this.time = time;
            this.previousState = previousState;
            this.currentState = currentState;

            this.probabilityIndicator = ProbabilityIndicator.Calculate(previousState, currentState, thresholds);
            this.surprise = Calculate(probabilityIndicator);
        }

        private double Calculate(double probabilityIndicator)
        {
            return -Math.Log(probabilityIndicator);
        }

        public double Value => surprise;
        public DateTime Time => time;

        public override string ToString()
        {
            return $"{Time}, P={probabilityIndicator}, S={Value}";
        }

        internal void MoveTime(TimeSpan timeSpan)
        {
            time = time + timeSpan;
        }
    }
}
