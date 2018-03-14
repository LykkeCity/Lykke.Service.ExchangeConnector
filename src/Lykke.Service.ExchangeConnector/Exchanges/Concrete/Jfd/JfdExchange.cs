using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using QuickFix.Fields;
using QuickFix.FIX44;
using Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient;
using Lykke.ExternalExchangesApi.Shared;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;
using ExecType = TradingBot.Trading.ExecType;
using ExecutionReport = TradingBot.Trading.ExecutionReport;
using ILog = Common.Log.ILog;
using TimeInForce = QuickFix.Fields.TimeInForce;

namespace TradingBot.Exchanges.Concrete.Jfd
{
    internal class JfdExchange : Exchange
    {
        private readonly JfdTradeSessionConnector _connector;
        private readonly JfdOrderBooksHarvester _harvester;
        private readonly IHandler<ExecutionReport> _executionHandler;
        private readonly JfdModelConverter _modelConverter;
        private readonly ILog _log;
        public new const string Name = "jfd";

        public JfdExchange(JfdExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, JfdOrderBooksHarvester harvester, IHandler<ExecutionReport> executionHandler, ILog log) : base(Name, config, translatedSignalsRepository, log)
        {
            _connector = new JfdTradeSessionConnector(new FixConnectorConfiguration(config.Password, config.GetTradingFixConfigAsReader()), log);
            _harvester = harvester;
            _executionHandler = executionHandler;
            _modelConverter = new JfdModelConverter(config);
            _log = log.CreateComponentScope(nameof(JfdExchange));
            harvester.MaxOrderBookRate = config.MaxOrderBookRate;
        }

        protected override void StartImpl()
        {
            _connector.Start();
            _harvester.Start();
            OnConnected();
        }

        protected override void StopImpl()
        {
            _connector.Stop();
            _harvester.Stop();
            OnStopped();
        }

        public override async Task<ExecutionReport> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var newOrderSingle = new NewOrderSingle
            {
                HandlInst = new HandlInst(HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE),
                Symbol = _modelConverter.ConvertLykkeSymbol(signal.Instrument.Name),
                Side = _modelConverter.ConvertSide(signal.TradeType),
                OrderQty = new OrderQty(signal.Volume),
                OrdType = _modelConverter.ConverOrderType(signal.OrderType),
                TimeInForce = new TimeInForce(TimeInForce.IMMEDIATE_OR_CANCEL),
                TransactTime = new TransactTime(DateTime.UtcNow),
                Price = new Price(signal.Price ?? 0m)
            };

            var cts = new CancellationTokenSource(timeout);
            var report = await _connector.AddOrderAsync(newOrderSingle, cts.Token);


            var trade = _modelConverter.ConvertExecutionReport(report);
            try
            {
                var handlerTrade = _modelConverter.ConvertExecutionReport(report);
                handlerTrade.ExecType = ExecType.Trade;
                await _executionHandler.Handle(handlerTrade);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(AddOrderAndWaitExecution), "Posting order to Jfd", ex);
            }
            return trade;
        }

        public override async Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout)
        {
            var pr = new CollateralInquiry()
            {
                NoPartyIDs = new NoPartyIDs(1)
            };

            var partyGroup = new CollateralInquiry.NoPartyIDsGroup
            {
                PartyID = new PartyID("*")
            };
            pr.AddGroup(partyGroup);
            var cts = new CancellationTokenSource(timeout);

            var collateral = await _connector.GetCollateralAsync(pr, cts.Token);

            var models = _modelConverter.ConvertCollateral(collateral);
            return models;
        }

        public override async Task<IReadOnlyCollection<PositionModel>> GetPositionsAsync(TimeSpan timeout)
        {
            var pr = new RequestForPositions
            {
                PosReqType = new PosReqType(PosReqType.POSITIONS),
                NoPartyIDs = new NoPartyIDs(1),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };
            var partyGroup = new RequestForPositions.NoPartyIDsGroup
            {
                PartyID = new PartyID("*")
            };
            pr.AddGroup(partyGroup);
            var cts = new CancellationTokenSource(timeout);
            var reports = await _connector.GetPositionsAsync(pr, cts.Token);

            return _modelConverter.ConvertPositionReport(reports);
        }

        public override StreamingSupport StreamingSupport => new StreamingSupport(true, false);

        public override Task<ExecutionReport> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }
}
