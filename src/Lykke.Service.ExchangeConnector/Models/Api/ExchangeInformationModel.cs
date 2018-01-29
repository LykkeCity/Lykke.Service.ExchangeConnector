using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;

namespace TradingBot.Models.Api
{
    /// <summary>
    /// Information about the exchange
    /// </summary>
    public sealed class ExchangeInformationModel
    {
        /// <summary>
        /// A name of the exchange
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Supported instruments
        /// </summary>
        public IEnumerable<Instrument> Instruments { get; set; }

        /// <summary>
        /// A current state of the exchange
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ExchangeState State { get; set; }

        /// <summary>
        /// A description of the exchange streaming capabilities
        /// </summary>
        public StreamingSupport StreamingSupport { get; set; }
    }

    /// <summary>
    /// A description of the exchange streaming capabilities
    /// </summary>
    public sealed class StreamingSupport
    {
        /// <summary>
        /// Can stream order books
        /// </summary>
        public bool OrderBooks { get; }

        /// <summary>
        /// Can stream orders updates
        /// </summary>
        public bool Orders { get; }

        /// <summary>
        /// Can stream position updates
        /// </summary>
        public bool Positions { get; }

        public StreamingSupport(bool orderBooks, bool orders, bool positions)
        {
            OrderBooks = orderBooks;
            Orders = orders;
            Positions = positions;
        }
    }
}
