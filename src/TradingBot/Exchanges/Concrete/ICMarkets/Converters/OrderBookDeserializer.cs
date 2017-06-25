using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.ICMarkets.Entities;
using System.Text;

namespace TradingBot.Exchanges.Concrete.ICMarkets.Converters
{
    public class OrderBookDeserializer : IMessageDeserializer<OrderBook>
    {
        public OrderBook Deserialize(byte[] data)
        {
            var serialized = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<OrderBook>(serialized);
        }
    }
}