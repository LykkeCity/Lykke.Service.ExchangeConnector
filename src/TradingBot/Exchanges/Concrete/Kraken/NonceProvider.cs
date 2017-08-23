using System;

namespace TradingBot.Exchanges.Concrete.Kraken
{
    public class NonceProvider
    {
        private readonly DateTime initialDateTime = new DateTime(2017, 08, 22, 0, 0, 0);

        private long lastNonce;
        
        public long GetNonce()
        {
            var now = DateTime.UtcNow;

            var nonce = (now - initialDateTime).Ticks;

            if (lastNonce >= nonce)
            {
                nonce = lastNonce + 1;
            }

            return lastNonce = nonce;
        }
    }
}