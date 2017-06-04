using System;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TradingBot.Infrastructure;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Communications
{
    public class RabbitMQClient : IDisposable
    {
        private readonly ILogger Logger = Logging.CreateLogger<RabbitMQClient>();

        public RabbitMQClient(RabbitMQConfiguration config)
        {
            this.queueName = config.QueueName;
            this.host = config.Host;

            factory = new ConnectionFactory()
	            {
	                HostName = host
	            };
        }

		private readonly string host;
        private readonly string queueName;

        private IConnection connection;
        private IModel channel;

        private ConnectionFactory factory;

        public bool OpenConnection()
        {
            try
            {
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                    
                channel.QueueDeclare(queue: queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                
                return true;
                    
            }
            catch (Exception ex)
            {
                Logger.LogError(new EventId(), ex, $"Can't connect to RabbitMQ host: {host}");
                return false;
            }
        }

        public void SendMessage(string message)
        {
			var body = Encoding.UTF8.GetBytes(message);

			channel.BasicPublish(exchange: "",
								 routingKey: queueName,
								 basicProperties: null,
								 body: body);
        }

        public void Dispose()
        {
            channel?.Dispose();
            connection?.Dispose();
        }
    }
}
