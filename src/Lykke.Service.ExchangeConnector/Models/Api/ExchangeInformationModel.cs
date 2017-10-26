using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;

namespace TradingBot.Models.Api
{
    public class ExchangeInformationModel
    {
        public string Name { get; set; }
        
        public IEnumerable<Instrument> Instruments { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ExchangeState State { get; set; }
    }
}
