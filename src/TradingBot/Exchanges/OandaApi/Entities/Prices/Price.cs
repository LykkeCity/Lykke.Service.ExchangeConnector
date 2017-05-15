using System;
using System.Collections.Generic;

namespace TradingBot.Exchanges.OandaApi.Entities.Prices
{
    /// <summary>
    /// The specification of an Account-specific Price.
    /// http://developer.oanda.com/rest-live-v20/pricing-df/#Price
    /// </summary>
    public class Price
    {
        /// <summary>
        /// The Price’s Instrument.
        /// </summary>
        public string Instrument { get; set; }
        
        /// <summary>
        /// The date/time when the Price was created
        /// </summary>
        public DateTime Time { get; set; }
        
        /// <summary>
        /// Flag indicating if the Price is tradeable or not
        /// </summary>
        public bool Tradeable { get; set; }
        
        /// <summary>
        /// The list of prices and liquidity available on the Instrument’s bid side.
        /// It is possible for this list to be empty if there is no bid liquidity
        /// currently available for the Instrument in the Account.
        /// </summary>
        public List<PriceBucket> Bids { get; set; }
        
        /// <summary>
        /// The list of prices and liquidity available on the Instrument’s ask side.
        /// It is possible for this list to be empty if there is no ask liquidity
        /// currently available for the Instrument in the Account.
        /// </summary>
        public List<PriceBucket> Asks { get; set; }
        
        /// <summary>
        /// The closeout bid Price. This Price is used when a bid is required to
        /// closeout a Position (margin closeout or manual) yet there is no bid
        /// liquidity. The closeout bid is never used to open a new position.
        /// </summary>
        public decimal CloseoutBid { get; set; }
        
        /// <summary>
        /// The closeout ask Price. This Price is used when a ask is required to
        /// closeout a Position (margin closeout or manual) yet there is no ask
        /// liquidity. The closeout ask is never used to open a new position.
        /// </summary>
        public decimal CloseoutAsk { get; set; }

        public override string ToString()
        {
            return $"{Time.ToString("HH:mm:ss.fff")} {Instrument}: {CloseoutBid} / {CloseoutAsk}";
        }
    }
}
