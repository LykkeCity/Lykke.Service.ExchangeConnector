using System;

namespace TradingBot.Communications
{
    public class InverseDateTimeRowKeyProvider
    {
        private long lastRawKey = long.MaxValue;

        private readonly object syncRoot = new object();

        public long GetNextRowKey()
        {
            lock (syncRoot)
            {
                long ticks = (DateTime.MaxValue - DateTime.UtcNow).Ticks;

                if (lastRawKey <= ticks)
                {
                    ticks = lastRawKey - 1;
                }

                return lastRawKey = ticks;
            }
        }
    }
}