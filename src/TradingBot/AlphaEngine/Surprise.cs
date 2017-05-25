using System;

namespace TradingBot.AlphaEngine
{
    public class Surprise
    {
        private DateTime time;
        private double probabilityIndicator;
        private double surprise;

        public Surprise(DateTime time, double probabilityIndicator)
        {
            this.time = time;
            this.probabilityIndicator = probabilityIndicator;
            this.surprise = Calculate();
        }

        public double Calculate()
        {
            return -Math.Log(probabilityIndicator);
        }

        public double Value => surprise;
        public DateTime Time => time;
    }
}
