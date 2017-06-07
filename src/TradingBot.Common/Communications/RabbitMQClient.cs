﻿using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TradingBot.Common.Configuration;
using TradingBot.Common.Infrastructure;

namespace TradingBot.Common.Communications
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
		private EventingBasicConsumer consumer;

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

				consumer = new EventingBasicConsumer(channel);

				channel.BasicConsume(
					queue: queueName,
					autoAck: false,
					consumer: consumer);

				return true;

			}
			catch (Exception ex)
			{
				Logger.LogError(new EventId(), ex, $"Can't connect to RabbitMQ host: {host}");
				return false;
			}
		}

		public void AddConsumer(Action<byte[]> messageCallback)
		{
			consumer.Received += (model, ea) =>
			{
				var body = ea.Body;
				messageCallback(body);
			};
		}

        public void SendMessage(object message)
        {
            SendMessage(JsonConvert.SerializeObject(message));
        }

		public void SendMessage(string message)
		{
            SendMessage(Encoding.UTF8.GetBytes(message));
		}

        public void SendMessage(byte[] bytes)
        {
            channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: bytes);
        }

		public void Dispose()
		{
			channel?.Dispose();
			connection?.Dispose();
		}
	}
}
