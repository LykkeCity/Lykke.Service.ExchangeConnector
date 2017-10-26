using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Instruments;

namespace TradingBot.Exchanges.Concrete.Oanda.Entities.Accounts
{
    public class AccountInstruments
    {
        public List<Instrument> Instruments { get; set; }

        public int LastTransactionId { get; set; }
    }
}
