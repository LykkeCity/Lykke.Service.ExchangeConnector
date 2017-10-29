using System.Text;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TradingBot.Trading
{
    public class GenericRabbitModelConverter<T> : IRabbitMqSerializer<T>, IMessageDeserializer<T>
    {
        public byte[] Serialize(T model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fff"
            }));
        }

        public T Deserialize(byte[] data)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data), new JsonSerializerSettings()
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fff"
            });
        }
    }
}
