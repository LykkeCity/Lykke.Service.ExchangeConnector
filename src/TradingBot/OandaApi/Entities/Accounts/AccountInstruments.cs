using System.Collections.Generic;
using TradingBot.OandaApi.Entities.Instruments;

namespace TradingBot.OandaApi.Entities.Accounts
{
    public class AccountInstruments
    {
        public List<Instrument> Instruments { get; set; }

        public int LastTransactionId { get; set; }
    }
}
