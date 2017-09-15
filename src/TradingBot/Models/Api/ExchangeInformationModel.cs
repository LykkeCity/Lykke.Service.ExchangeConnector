using System.Collections.Generic;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;

namespace TradingBot.Models.Api
{
    public class ExchangeInformationModel
    {
        public string Name { get; set; }
        
        public IEnumerable<Instrument> Instruments { get; set; }
        
        public ExchangeState State { get; set; }
    }
}