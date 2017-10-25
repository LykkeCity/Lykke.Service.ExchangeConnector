namespace TradingBot.Infrastructure.Configuration
{
    public sealed class OrderBookRabbitMqConfiguration : RabbitMqConfigurationBase
    {
        public string Exchange { get; set; }

        public bool Durable { get; set; }
    }
}
