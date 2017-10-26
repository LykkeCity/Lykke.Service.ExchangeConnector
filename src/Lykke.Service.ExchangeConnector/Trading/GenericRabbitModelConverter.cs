using System.Text;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TradingBot.Trading
{
    internal sealed class GenericRabbitModelConverter<T> : IRabbitMqSerializer<T>, IMessageDeserializer<T>
    {
        private const string Iso8601DateFormat = @"yyyy-MM-ddTHH:mm:ss.fffzzz";
        private readonly JsonSerializerSettings _serializeSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = Iso8601DateFormat
        };

        private readonly JsonSerializerSettings _deserializeSettings = new JsonSerializerSettings
        {
            DateFormatString = Iso8601DateFormat
        };

        public byte[] Serialize(T model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model, _serializeSettings));
        }

        public T Deserialize(byte[] data)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data), _deserializeSettings);
        }
    }
}
