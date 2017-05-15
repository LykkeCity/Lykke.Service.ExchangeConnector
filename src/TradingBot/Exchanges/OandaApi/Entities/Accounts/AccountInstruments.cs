using System.Collections.Generic;
using TradingBot.Exchanges.OandaApi.Entities.Instruments;

namespace TradingBot.Exchanges.OandaApi.Entities.Accounts
{
    public class AccountInstruments
    {
        public List<Instrument> Instruments { get; set; }

        public int LastTransactionId { get; set; }
    }
}
