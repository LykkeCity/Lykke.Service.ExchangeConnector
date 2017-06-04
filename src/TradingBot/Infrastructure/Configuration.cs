using System.Collections.Generic;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;

namespace TradingBot.Infrastructure
{
    public class Configuration
    {
        public Exchange Exchange { get; set; }

        public List<Instrument> Instruments { get; set; }

        public RabbitMQConfiguration RabbitMQConfig { get; set; }

        public static Configuration CreateDefaultConfig()
        {
            Exchange defaultExchange = new Exchanges.Concrete.Kraken.KrakenExchange();
            Instrument defaultInstrument = new Instrument("XXBTZUSD");

            return new Configuration()
            {
                Exchange = defaultExchange,
                Instruments = new List<Instrument>() { defaultInstrument },
                RabbitMQConfig = new RabbitMQConfiguration()
                {
                    Host = "rabbit",
                    QueueName = $"{defaultExchange.Name}.{defaultInstrument.Name}"
                }
            };
        }
    }

    public class RabbitMQConfiguration
    {
        public string Host { get; set; }

        public string QueueName { get; set; }
    }
}
