using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal class IcmExchange : Exchange
    {
        private readonly ILog _log;
        private readonly IcmConfig _config;
        private readonly IcmTickPriceHarvester _tickPriceHarvester;

        private readonly IIcmConnector _connector;
        public new static readonly string Name = "icm";

        public IcmExchange(IcmTickPriceHarvester tickPriceHarvester,
            IIcmConnector connector,
            IcmConfig config,
            TranslatedSignalsRepository translatedSignalsRepository,
            ILog log)
            : base(Name, config, translatedSignalsRepository, log)
        {
            _config = config;
            _tickPriceHarvester = tickPriceHarvester;
            _log = log;
            _connector = connector;

            _connector.Connected += OnConnected;
            _connector.Disconnected += OnStopped;


        }

        protected override void StartImpl()
        {
            if (_config.SocketConnection)
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "Socket connection is enabled");
                StartFixConnection();
            }
            else
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "Socket connection is disabled");
            }

            if (_config.RabbitMq.Enabled && (_config.PubQuotesToRabbit))
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "RabbitMQ connection is enabled");
                StartRabbitConnection();
            }
            else
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "RabbitMQ connection is disabled");
            }
        }

        protected override void StopImpl()
        {
            _connector.Stop();
            _tickPriceHarvester.Stop();
        }

        private void StartFixConnection()
        {
            _connector.Start();
            _tickPriceHarvester.Start();
        }

        /// <summary>
        /// For ICM we use internal RabbitMQ exchange with pricefeed
        /// </summary>
        private void StartRabbitConnection() //HACK Must be deleted from here!
        {

        }

        public override Task<ExecutionReport> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            return _connector.GetOrderInfoAndWaitResponse(instrument, id);
        }

        public override Task<IEnumerable<ExecutionReport>> GetOpenOrders(TimeSpan timeout)
        {
            return _connector.GetAllOrdersInfo(timeout);
        }

        public override Task<ExecutionReport> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            return _connector.AddOrderAndWaitResponse(signal, translatedSignal, timeout);
        }

        public override Task<ExecutionReport> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            return _connector.CancelOrderAndWaitResponse(signal, translatedSignal, timeout);
        }
    }
}
