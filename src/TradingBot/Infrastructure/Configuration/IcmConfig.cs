namespace TradingBot.Infrastructure.Configuration
{
    public class IcmConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string Username { get; set; }

        public string Password { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string[] Instruments { get; set; }

        public RabbitMqConfig RabbitMq { get; set; }
    }

    public class RabbitMqConfig
    {
        public string Host { get; set; }
        
        public int Port { get; set; }
        
        public string Exchange { get; set; }
        
        public string Username { get; set; }
        
        public string Password { get; set; }
        
        /// <summary>
        /// see https://www.rabbitmq.com/uri-spec.html
        /// </summary>
        public string GetConnectionString()
        {
            var amqpAuthority = string.Empty;

            if (!string.IsNullOrEmpty(Username))
            {
                amqpAuthority += Username;
                
                if (!string.IsNullOrEmpty(Password))
                    amqpAuthority += $":{Password}";
                
                amqpAuthority += "@";
            }
            
            return $"amqp://{amqpAuthority}{Host}:{Port}";
        }
    }
}
