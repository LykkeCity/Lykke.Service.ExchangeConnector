namespace TradingBot.Infrastructure.Configuration
{
    public sealed class RabbitMqSingleExchangeConfiguration : RabbitMqConfigurationBase
    {
        public string Exchange { get; set; }

        public bool Durable { get; set; }
    }
}
