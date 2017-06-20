namespace TradingBot.Common.Configuration
{
    public class RabbitMqConfiguration
	{
        public bool Enabled { get; set; }

		public string Host { get; set; }

		public string QueueName { get; set; }
		
		public string ExchangeName { get; set; }
    }
}
