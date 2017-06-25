using System.Text;
using Lykke.RabbitMqBroker.Publisher;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Abstractions
{
    public class InstrumentTickPricesSerializer : IRabbitMqSerializer<InstrumentTickPrices>
    {
        public byte[] Serialize(InstrumentTickPrices model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
        }
    }
}