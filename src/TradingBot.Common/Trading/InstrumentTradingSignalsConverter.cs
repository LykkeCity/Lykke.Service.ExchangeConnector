using System.Text;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;

namespace TradingBot.Common.Trading
{
    public class InstrumentTradingSignalsConverter : 
        IRabbitMqSerializer<InstrumentTradingSignals>, 
        IMessageDeserializer<InstrumentTradingSignals>
    {
        public byte[] Serialize(InstrumentTradingSignals model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
        }

        public InstrumentTradingSignals Deserialize(byte[] data)
        {
            return JsonConvert.DeserializeObject<InstrumentTradingSignals>(Encoding.UTF8.GetString(data));
        }
    }
}