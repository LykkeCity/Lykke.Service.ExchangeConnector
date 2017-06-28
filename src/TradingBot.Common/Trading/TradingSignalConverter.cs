using System.Text;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;

namespace TradingBot.Common.Trading
{
    public class TradingSignalConverter: IRabbitMqSerializer<TradingSignal>, IMessageDeserializer<TradingSignal>
    {
        public byte[] Serialize(TradingSignal model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
        }

        public TradingSignal Deserialize(byte[] data)
        {
            return JsonConvert.DeserializeObject<TradingSignal>(Encoding.UTF8.GetString(data));
        }
    }
}