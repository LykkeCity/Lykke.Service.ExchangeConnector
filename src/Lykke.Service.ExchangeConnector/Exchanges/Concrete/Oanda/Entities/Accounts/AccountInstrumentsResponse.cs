using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Instruments;

namespace TradingBot.Exchanges.Concrete.Oanda.Entities.Accounts
{
    /// <summary>
    /// The list of tradeable instruments for the Account has been provided.
    /// </summary>
    public class AccountInstrumentsResponse
    {
        /// <summary>
        /// The requested list of instruments.
        /// </summary>
        public List<Instrument> Instruments { get; set; }
        
        /// <summary>
        /// The ID of the most recent Transaction created for the Account.
        /// </summary>
        public int LastTransactionID { get; set; }
    }
}
