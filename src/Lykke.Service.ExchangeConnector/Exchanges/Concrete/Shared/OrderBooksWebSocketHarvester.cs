﻿using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal abstract class OrderBooksWebSocketHarvester<TRequest, TResponse> : OrderBooksHarvesterBase
    {
        protected IMessenger<TRequest, TResponse> Messenger;

        protected OrderBooksWebSocketHarvester(string exchangeName, IExchangeConfiguration exchangeConfiguration, IMessenger<TRequest, TResponse> messanger, ILog log,
            IHandler<OrderBook> orderBookHandler)
            : base(exchangeName, exchangeConfiguration, log, orderBookHandler)
        {
            Messenger = messanger;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (Messenger != null)
                {
                    Messenger.Dispose();
                    Messenger = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
