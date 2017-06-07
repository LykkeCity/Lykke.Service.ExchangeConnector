using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Common.Infrastructure;

namespace TradingBot.Exchanges.Concrete.ICMarkets
{
    public class RabbitMQClient
    {
        private static ILogger Logger = Logging.CreateLogger<RabbitMQClient>();

        public async Task OpenConnection(CancellationToken cancellationToken,
            Action<byte[]> callback)
        {
            var factory = new ConnectionFactory()
            {
                 HostName = RabbitMQAuth.Host,
                 UserName = RabbitMQAuth.UserName,
                 Password = RabbitMQAuth.Password
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: RabbitMQAuth.ExchangeName, type: "fanout");

                    var queueName = channel.QueueDeclare().QueueName;

                    channel.QueueBind(
                        queue: queueName,
                        exchange: RabbitMQAuth.ExchangeName,
                        routingKey: "");

                    Logger.LogInformation("Connection to RabbitMQ established");


                    var consumer = new EventingBasicConsumer(channel);

                    consumer.Received += (sender, eventArgs) => {
                        callback(eventArgs.Body);
                    };

                    channel.BasicConsume(queueName, true, consumer);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }

                    Logger.LogInformation("Canellation requested.");
                }
            }
        }
    }
}
