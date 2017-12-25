using Autofac;
using Common.Log;

namespace TradingBot.Handlers
{
    internal interface IRabbitMqHandlersFactory
    {
        IHandler<T> Create<T>(string connectionString, string exchangeName, bool durable = false, ILog log = null);
    }

    internal class RabbitMqHandlersFactory : IRabbitMqHandlersFactory
    {
        private readonly IComponentContext  _container;

        public RabbitMqHandlersFactory(IComponentContext  container)
        {
            _container = container;
        }

        public IHandler<T> Create<T>(string connectionString, string exchangeName, bool durable = false, ILog log = null)
        {
            return _container.Resolve<RabbitMqHandler<T>>(new NamedParameter("connectionString", connectionString),
                    new NamedParameter("exchangeName", exchangeName),
                    new NamedParameter("durable", durable),
                    new NamedParameter("log", log));
        }
    }
}
