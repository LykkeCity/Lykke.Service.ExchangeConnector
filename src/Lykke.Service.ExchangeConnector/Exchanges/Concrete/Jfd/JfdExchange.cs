using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using QuickFix.Fields;
using QuickFix.FIX44;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Jfd.FixClient;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;
using ILog = Common.Log.ILog;
using TimeInForce = QuickFix.Fields.TimeInForce;

namespace TradingBot.Exchanges.Concrete.Jfd
{
    internal class JfdExchange : Exchange
    {
        private readonly JfdExchangeConfiguration _config;
        private readonly JfdTradeSessionConnector _connector;
        private readonly JfdOrderBooksHarvester _harvester;
        private readonly JfdModelConverter _modelConverter;
        private readonly ILog _log;
        public new const string Name = "jfd";

        public JfdExchange(JfdExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, JfdTradeSessionConnector connector, JfdOrderBooksHarvester harvester, ILog log) : base(Name, config, translatedSignalsRepository, log)
        {
            _config = config;
            _connector = connector;
            _harvester = harvester;
            _modelConverter = new JfdModelConverter(config);
            _log = log.CreateComponentScope(nameof(JfdExchange));
            harvester.MaxOrderBookRate = config.MaxOrderBookRate;
            harvester.AddHandler(CallOrderBookHandlers);
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

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var newOrderSingle = new NewOrderSingle
            {
                //  NoPartyIDs = new NoPartyIDs(0),
                HandlInst = new HandlInst(HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE),
                Symbol = _modelConverter.ConvertLykkeSymbol(signal.Instrument.Name),
                Side = _modelConverter.ConvertSide(signal.TradeType),
                OrderQty = new OrderQty(signal.Volume),
                OrdType = _modelConverter.ConvertType(signal.OrderType),
                TimeInForce = new TimeInForce(TimeInForce.IMMEDIATE_OR_CANCEL),
                TransactTime = new TransactTime(DateTime.UtcNow)
            };

            var cts = new CancellationTokenSource(timeout);
            var report = await _connector.AddOrderAsync(newOrderSingle, cts.Token);


            var trade = ConvertExecutionReport(report);
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

            var models = ConvertCollateral(collateral);
            return models;
        }

        public override async Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout)
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

            return ConvertPositionReport(reports);
        }

        public override Task<ExecutedTrade> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        private IReadOnlyCollection<PositionModel> ConvertPositionReport(IReadOnlyCollection<PositionReport> reports)
        {
            var result = new List<PositionModel>(reports.Count);
            foreach (var report in reports)
            {
                var inst = _modelConverter.ConvertJfdSymbol(report.Symbol);
                var details = report.GetGroup(1, new PositionReport.NoPositionsGroup());
                var longQty = details.GetField(new LongQty()).Obj;
                var shortQty = details.GetField(new ShortQty()).Obj;
                var usedMagin = details.IsSetField(OneZeroCustomTag.OzUsedMargin) ? details.GetDecimal(OneZeroCustomTag.OzUsedMargin) : 0m;
                var unrealizedPnl = details.IsSetField(OneZeroCustomTag.OzUnrealizedProfitOrLoss) ? details.GetDecimal(OneZeroCustomTag.OzUnrealizedProfitOrLoss) : 0m;

                var setting = _config.SupportedCurrencySymbols.FirstOrDefault(i => string.Equals(i.ExchangeSymbol, inst.Name, StringComparison.InvariantCultureIgnoreCase));
                var initialMargin = setting?.InitialMarginPercent ?? -1;
                var maintMargin = setting?.MaintMarginPercent ?? -1;

                var model = new PositionModel
                {
                    Symbol = inst.Name,
                    PositionVolume = longQty - shortQty,
                    MaintMarginUsed = usedMagin,
                    RealisedPnL = 0,
                    UnrealisedPnL = unrealizedPnl,
                    PositionValue = null,
                    AvailableMargin = null,
                    InitialMarginRequirement = initialMargin,
                    MaintenanceMarginRequirement = maintMargin,
                };
                result.Add(model);
            }
            return result;
        }

        private ExecutedTrade ConvertExecutionReport(ExecutionReport report)
        {
            var inst = _modelConverter.ConvertJfdSymbol(report.Symbol);
            var time = report.TransactTime.Obj;
            var price = report.LastPx.Obj;
            var volume = report.LastQty.Obj;
            var type = _modelConverter.ConvertSide(report.Side);
            var id = report.OrderID.Obj;
            var status = _modelConverter.ConvertStatus(report.OrdStatus);

            var executedTrade = new ExecutedTrade(inst, time, price, volume, type, id, status);
            return executedTrade;
        }


        private static IReadOnlyCollection<TradeBalanceModel> ConvertCollateral(IEnumerable<CollateralReport> collateralReports)
        {

            var models = new List<TradeBalanceModel>();
            foreach (var report in collateralReports)
            {
                var instr = report.IsSetField(OneZeroCustomTag.OzAccountCurrency) ? report.GetString(OneZeroCustomTag.OzAccountCurrency) : "NoSymbol";
                var model = new TradeBalanceModel
                {
                    AccountCurrency = instr,
                    Totalbalance = report.IsSetField(OneZeroCustomTag.OzEquity) ? report.GetDecimal(OneZeroCustomTag.OzEquity) : -1,
                    UnrealisedPnL = report.IsSetField(OneZeroCustomTag.OzUnrealizedProfitOrLoss) ? report.GetDecimal(OneZeroCustomTag.OzUnrealizedProfitOrLoss) : -1,
                    MaringAvailable = report.IsSetField(OneZeroCustomTag.OzUsedMargin) ? report.GetDecimal(OneZeroCustomTag.OzUsedMargin) : -1,
                    MarginUsed = report.IsSetField(OneZeroCustomTag.OzUsedMargin) ? report.GetDecimal(OneZeroCustomTag.OzUsedMargin) : -1
                };
                models.Add(model);
            }

            return models;
        }

        private static class OneZeroCustomTag
        {
            public const int OzAccountCurrency = 8880;
            public const int OzAccountBalance = 8881;
            public const int OzMarginUtilizationPercentage = 8882;
            public const int OzUsedMargin = 8883;
            public const int OzFreeMargin = 8884;
            public const int OzUnrealizedProfitOrLoss = 8885;
            public const int OzEquity = 8886;
        }
    }
}
