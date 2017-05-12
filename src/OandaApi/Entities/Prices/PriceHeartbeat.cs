using System;

namespace TradingBot.OandaApi.Entities.Prices
{
    /// <summary>
    /// A PricingHeartbeat object is injected into the Pricing stream to ensure that the HTTP connection remains active.
    /// http://developer.oanda.com/rest-live-v20/pricing-df/#PricingHeartbeat
    /// </summary>
    public class PriceHeartbeat
    {
        /// <summary>
        /// The date/time when the Heartbeat was created.
        /// </summary>
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return $"{Time.ToString("HH:mm:ss.fff")} Heartbeat";
        }
    }
}
