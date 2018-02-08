using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Lykke.Service.ExchangeDataStore.Services.Helpers
{
    internal sealed class GenericRabbitModelConverter<T> : IRabbitMqSerializer<T>, IMessageDeserializer<T>
    {
        private readonly JsonSerializerSettings _serializeSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        private readonly JsonSerializerSettings _deserializeSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
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
