using System.Text;
using Newtonsoft.Json;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace TradingBot.Common.Trading
{
    public class InstrumentTickPricesConverter : IRabbitMqSerializer<InstrumentTickPrices>, IMessageDeserializer<InstrumentTickPrices>
    {
        public byte[] Serialize(InstrumentTickPrices model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
        }

        public InstrumentTickPrices Deserialize(byte[] data)
        {
            return JsonConvert.DeserializeObject<InstrumentTickPrices>(Encoding.UTF8.GetString(data));
        }
    }
}
