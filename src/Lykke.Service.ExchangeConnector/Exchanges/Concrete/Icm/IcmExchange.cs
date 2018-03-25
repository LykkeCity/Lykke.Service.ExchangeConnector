using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Icm.FixClient;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using TradingBot.Repositories;
using ExecutionReport = TradingBot.Trading.ExecutionReport;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal class IcmExchange : Exchange
    {
        private readonly ILog _log;
        private readonly IcmExchangeConfiguration _config;
        private readonly IcmModelConverter _converter;
        private readonly IcmTickPriceHarvester _tickPriceHarvester;
        private readonly IcmTradeSessionConnector _tradeSessionConnector;

        public new static readonly string Name = "icm";

        public IcmExchange(IcmTickPriceHarvester tickPriceHarvester,

            IcmExchangeConfiguration config,
            TranslatedSignalsRepository translatedSignalsRepository,
            IcmModelConverter converter,
            ILog log)
            : base(Name, config, translatedSignalsRepository, log)
        {
            _config = config;
            _converter = converter;
            _tickPriceHarvester = tickPriceHarvester;
            _log = log;

            if (_config.SocketConnection)
            {
                _tradeSessionConnector = new IcmTradeSessionConnector(new FixConnectorConfiguration(config.Password, config.GetFixConfigAsReader()), log);
            }
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
            }
            else
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "RabbitMQ connection is disabled");
            }
        }

        protected override void StopImpl()
        {
            if (_config.SocketConnection)
            {
                _tradeSessionConnector.Stop();
            }
            _tickPriceHarvester.Stop();
            OnStopped();
        }

        private void StartFixConnection()
        {
            _tradeSessionConnector.Start();
            _tickPriceHarvester.Start();
            OnConnected();
        }

        public override async Task<ExecutionReport> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            var request = _converter.CreateNewOrderSingle(signal);
            var response = await _tradeSessionConnector.AddOrderAsync(request, cts.Token);
            return _converter.ConvertExecutionReport(response);
        }

        public override async Task<IReadOnlyCollection<PositionModel>> GetPositionsAsync(TimeSpan timeout)
        {
            var request = new RequestForPositions
            {
                PosReqID = new PosReqID(nameof(RequestForPositions) + Guid.NewGuid()),
                PosReqType = new PosReqType(PosReqType.POSITIONS),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT),
                NoPartyIDs = new NoPartyIDs(1),
                Account = new Account("account"),
                AccountType = new AccountType(AccountType.ACCOUNT_IS_CARRIED_ON_CUSTOMER_SIDE_OF_BOOKS),
                ClearingBusinessDate = new ClearingBusinessDate(DateTimeConverter.ConvertDateOnly(DateTime.UtcNow.Date)),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };

            var partyGroup = new RequestForPositions.NoPartyIDsGroup
            {
                PartyID = new PartyID("FB"),
                PartyRole = new PartyRole(PartyRole.CLIENT_ID)
            };

            request.AddGroup(partyGroup);
            var cts = new CancellationTokenSource(timeout);

            var resp = await _tradeSessionConnector.GetPositionsAsync(request, cts.Token);

            return _converter.ConvertPositionReport(resp);
        }

        public override StreamingSupport StreamingSupport => new StreamingSupport(true, false);

        public override Task<ExecutionReport> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
