using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Trading;
using TradingBot.Communications;
using TradingBot.Repositories;
using Message = QuickFix.Message;
using TradeType = TradingBot.Trading.TradeType;
using TimeInForce = TradingBot.Trading.TimeInForce;
using ILog = Common.Log.ILog;

namespace TradingBot.Exchanges.Concrete.Icm
{
    public class IcmConnector : IApplication
    {
        private readonly ILog logger;
        private readonly IcmConfig config;
        private readonly AzureFixMessagesRepository repository;

        private SessionID session;


        private readonly object orderIdsSyncRoot = new Object();
        private readonly Dictionary<string, string> orderIdsToIcmIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> orderIcmIdsToIds = new Dictionary<string, string>();

        public event Action Connected;
        public event Action Disconnected;
        public event Func<OrderStatusUpdate, Task> OnTradeExecuted;

        public IcmConnector(IcmConfig config, AzureFixMessagesRepository repository, ILog logger)
        {
            this.config = config;
            this.repository = repository;
            this.logger = logger;
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            var header = message.Header;

            if (header.GetString(Tags.MsgType) == MsgType.LOGON)
            {
                message.SetField(new Username(config.Username));
                message.SetField(new Password(config.Password));
            }

            repository.SaveMessage(message, FixMessageDirection.ToAdmin);
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, $"FromAdmin message: {message}").Wait();

            repository.SaveMessage(message, FixMessageDirection.FromAdmin);

            try
            {
                switch (message)
                {
                    case Reject reject:
                        HandleRejected(reject);
                        break;
                    default:
                        break;
                    case null:
                        logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, "Received null message").Wait();
                        break;
                }
            }
            catch (Exception e)
            {
                logger.WriteErrorAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, e).Wait();
            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(ToApp), string.Empty, $"Outgoing (ToApp) message is sent: {message}").Wait();
            repository.SaveMessage(message, FixMessageDirection.ToApp);
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, $"FromApp message: {message}").Wait();
            repository.SaveMessage(message, FixMessageDirection.FromApp);

            try
            {
                switch (message)
                {
                    case ExecutionReport executionReport:
                        HandleExecutionReport(executionReport);
                        break;
                    case SecurityList securityList:
                        HandleSecurityList(securityList);
                        break;
                    case PositionReport positionReport:
                        HandlePositionReport(positionReport);
                        break;
                    case MarketDataIncrementalRefresh marketDataIncrementalRefresh:
                        logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromApp), string.Empty, "Market data incremental refresh is not supported. We read the data from RabbitMQ").Wait();
                        break;
                    case ListStatus listStatus:
                        HandleListStatus(listStatus);
                        break;
                    default:
                        break;
                    case null:
                        logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromApp), string.Empty, "Received null message").Wait();
                        break;
                }
            }
            catch (Exception e)
            {
                logger.WriteErrorAsync(nameof(IcmConnector), nameof(FromApp), string.Empty, e).Wait();
            }
        }

        public void OnCreate(SessionID sessionID)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(OnCreate), string.Empty, $"Session created {sessionID}").Wait();
        }

        public void OnLogout(SessionID sessionID)
        {
            session = null;
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(OnLogout), string.Empty, $"Logout session {sessionID}").Wait();

            Disconnected?.Invoke();
        }

        public void OnLogon(SessionID sessionID)
        {
            session = sessionID;
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(OnLogon), string.Empty, $"Logon for session {sessionID}").Wait();

            SendRequestForPositions();
            SendSecurityListRequest();
            //SendOrderStatusRequest();

            Connected?.Invoke();
        }

        private void HandleSecurityList(SecurityList securityList)
        {
            var securitiesGruop = new SecurityList.NoRelatedSymGroup();

            var sb = new StringBuilder();

            for (int i = 1; i <= securityList.NoRelatedSym.Obj; i++)
            {
                securityList.GetGroup(i, securitiesGruop);

                sb.Append($"{securitiesGruop.Symbol}. ");

                var attrGroup = new SecurityList.NoRelatedSymGroup.NoInstrAttribGroup();

                for (int j = 1; j <= securitiesGruop.NoInstrAttrib.Obj; j++)
                {
                    securitiesGruop.GetGroup(j, attrGroup);

                    if (attrGroup.InstrAttribType.Obj == 18) // Forex Instrument decimal places in ICM
                    {
                        sb.AppendLine($"Decimal places: {attrGroup.InstrAttribValue}");
                    }
                }
            }

            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleSecurityList), string.Empty, sb.ToString()).Wait();
        }

        private void HandlePositionReport(PositionReport positionReport)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"Position report received: {positionReport}. Base currency: {positionReport.Currency.Obj}, Symbol: {positionReport.Symbol.Obj}").Wait();

            // 8=FIX.4.49=314
            // 35=AP
            // 34=60
            // 49=TS
            // 52=20170717-09:28:13.431
            // 56=ICMD50000395d-TRADE
            // 1=Margin Account
            // 15=EUR
            // 55=EUR/USDm
            // 263=0
            // 581=1710=636358804932330300721=0727=1
            // 728=0 // valid request
            // 730=1.06560914
            // 731=1 // final
            // 453=1448=TS452=3
            // 702=1
            //   703=OPN
            //   704=91000
            // 753=5707=VADJ708=0.000707=IMTM708=7217.280707=TVAR708=33707=PREM708=44707=CRES708=5510=155


            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"Number of positions: {positionReport.NoPositions.Obj}");

            var positionsGroup = new PositionReport.NoPositionsGroup();

            for (int i = 1; i <= positionReport.NoPositions.Obj; i++)
            {
                positionReport.GetGroup(i, positionsGroup);

                if (positionsGroup.IsSetLongQty())
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"{i}: Long: {positionsGroup.LongQty.Obj}").Wait();
                else if (positionsGroup.IsSetShortQty())
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"{i}: Short: {positionsGroup.ShortQty.Obj}").Wait();
            }
        }


        private readonly Dictionary<string, string> symbolsMap = new Dictionary<string, string>()
        {
            { "DAX30", "#DAX30" },
            { "DOW20", "#DOW30" },
            { "CHFJPY", "CHF/JPYm" },
            { "AUDCAD", "AUD/CADm" },
            { "USDSGD", "USD/SGDm" },
            { "EURSGD", "EUR/SGDm" },
            { "NASDAQ100", "#NASDAQ100" },
            { "NZDJPY", "NZD/JPYm" },
            { "AUDCHF", "AUD/CHFm" },
            { "CADCHF", "CAD/CHFm" },
            { "AUDJPY", "AUD/JPYm" },
            { "GBPJPY", "GBP/JPYm" },
            { "AUDUSD", "AUD/USDm" },
            { "AUDNZD", "AUD/NZDm" },
            { "CADJPY", "CAD/JPYm" },
            { "EURAUD", "EUR/AUDm" },
            { "USDCAD", "USD/CADm" },
            { "EURCAD", "EUR/CADm" },
            { "USDCHF", "USD/CHFm" },
            { "EURCHF", "EUR/CHFm" },
            { "EURGBP", "EUR/GBPm" },
            { "USDJPY", "USD/JPYm" },
            { "EURJPY", "EUR/JPYm" },
            { "EURNZD", "EUR/NZDm" },
            { "EURUSD", "EUR/USDm" },
            { "GBPAUD", "GBP/AUDm" },
            { "GBPNZD", "GBP/NZDm" },
            { "GBPCAD", "GBP/CADm" },
            { "NZDCAD", "NZD/CADm" },
            { "GBPCHF", "GBP/CHFm" },
            { "GBPSGD", "GBP/SGDm" },
            { "GBPUSD", "GBP/USDm" },
            { "NZDCHF", "NZD/CHFm" },
            { "NZDUSD", "NZD/USDm" },
            { "USDCNH", "USD/CNHm" },
            { "XAGUSD", "XAG/USDm" },
            { "XAUUSD", "XAU/USDm" }
        };


        private readonly Dictionary<string, string> symbolsMapBack = new Dictionary<string, string>()
        {
            { "#DAX30", "DAX30" },
            { "#DOW30", "DOW20" },
            { "CHF/JPYm", "CHFJPY" },
            { "AUD/CADm", "AUDCAD" },
            { "USD/SGDm", "USDSGD" },
            { "EUR/SGDm", "EURSGD" },
            { "#NASDAQ100", "NASDAQ100"},
            { "NZD/JPYm", "NZDJPY" },
            { "AUD/CHFm", "AUDCHF" },
            { "CAD/CHFm", "CADCHF" },
            { "AUD/JPYm", "AUDJPY" },
            { "GBP/JPYm", "GBPJPY" },
            { "AUD/USDm", "AUDUSD" },
            { "AUD/NZDm", "AUDNZD" },
            { "CAD/JPYm", "CADJPY" },
            { "EUR/AUDm", "EURAUD" },
            { "USD/CADm", "USDCAD" },
            { "EUR/CADm", "EURCAD" },
            { "USD/CHFm", "USDCHF" },
            { "EUR/CHFm", "EURCHF" },
            { "EUR/GBPm", "EURGBP" },
            { "USD/JPYm", "USDJPY" },
            { "EUR/JPYm", "EURJPY" },
            { "EUR/NZDm", "EURNZD" },
            { "EUR/USDm", "EURUSD" },
            { "GBP/AUDm", "GBPAUD" },
            { "GBP/NZDm", "GBPNZD" },
            { "GBP/CADm", "GBPCAD" },
            { "NZD/CADm", "NZDCAD" },
            { "GBP/CHFm", "GBPCHF" },
            { "GBP/SGDm", "GBPSGD" },
            { "GBP/USDm", "GBPUSD" },
            { "NZD/CHFm", "NZDCHF" },
            { "NZD/USDm", "NZDUSD" },
            { "USD/CNHm", "USDCNH" },
            { "XAG/USDm", "XAGUSD" },
            { "XAU/USDm", "XAUUSD" }
        };

        private void HandleExecutionReport(ExecutionReport report)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, "Execution report was received").Wait();

            string id;
            string icmId;
            GetIds(report, out id, out icmId);

            OrderExecutionStatus orderExecutionStatus = OrderExecutionStatus.Pending;

            switch (report.ExecType.Obj)
            {
                case ExecType.REJECTED:
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"ICM rejected the order {id}. Rejection reason is {report.OrdRejReason}").Wait();
                    orderExecutionStatus = OrderExecutionStatus.Rejected;
                    break;

                case ExecType.TRADE:

                    if (report.OrdStatus.Obj == OrdStatus.FILLED)
                    {
                        logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"ICM filled the order {id}! OrdType: {report.OrdStatus.Obj}").Wait();
                        orderExecutionStatus = OrderExecutionStatus.Fill;
                    }
                    else if (report.OrdStatus.Obj == OrdStatus.PARTIALLY_FILLED)
                    {
                        logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"ICM filled the order {id} partially.").Wait();
                        orderExecutionStatus = OrderExecutionStatus.PartialFill;
                    }
                    break;

                case ExecType.NEW:
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Order {id} placed as new!").Wait();
                    orderExecutionStatus = OrderExecutionStatus.New;
                    break;

                case ExecType.CANCELLED:
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Order {id} was cancelled!").Wait();
                    orderExecutionStatus = OrderExecutionStatus.Cancelled;
                    break;

                case ExecType.ORDER_STATUS:
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Order status for {id}, side: {report.Side}, orderQty: {report.OrderQty}").Wait();
                    break;

                case ExecType.PENDING_CANCEL:
                    logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Cancellation of the order {id} is pending!").Wait();
                    break;
            }



            var executedTrade = new OrderStatusUpdate(
                new Instrument(IcmExchange.Name, report.IsSetField(Tags.Symbol) ? symbolsMapBack[report.Symbol.Obj] : ""),
                report.IsSetField(Tags.TransactTime) ? report.TransactTime.Obj : DateTime.UtcNow,
                orderExecutionStatus == OrderExecutionStatus.Fill || orderExecutionStatus == OrderExecutionStatus.PartialFill ? report.AvgPx.Obj : report.Price.Obj,
                report.OrderQty.Obj,
                report.Side.Obj == Side.BUY ? TradeType.Buy : TradeType.Sell,
                id,
                orderExecutionStatus);

            if (report.IsSetField(Tags.Text))
            {
                executedTrade.Message = report.Text.Obj;
            }

            if (orderExecutionStatus == OrderExecutionStatus.Cancelled ||
                orderExecutionStatus == OrderExecutionStatus.Fill ||
                orderExecutionStatus == OrderExecutionStatus.PartialFill)
            {
                OnTradeExecuted?.Invoke(executedTrade); // TODO: await?
            }


            lock (orderExecutions)
            {
                if (orderExecutions.ContainsKey(id))
                {
                    if (orderExecutions[id].Task.Status < TaskStatus.RanToCompletion)
                    {
                        orderExecutions[id].SetResult(executedTrade);
                    }
                    else
                    {
                        orderExecutions[id] = new TaskCompletionSource<OrderStatusUpdate>();
                        orderExecutions[id].SetResult(executedTrade);
                    }
                }
            }
        }

        private void GetIds(ExecutionReport report, out string id, out string icmId)
        {
            icmId = null;
            id = null;

            if (report.IsSetField(Tags.ClOrdID) && !report.ClOrdID.Obj.EndsWith("cancel"))
            {
                id = report.ClOrdID.Obj;
            }

            if (report.IsSetField(Tags.OrderID))
            {
                icmId = report.GetField(new OrderID()).Obj;
            }

            lock (orderIdsSyncRoot)
            {
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(icmId))
                {
                    if (!orderIdsToIcmIds.ContainsKey(id))
                    {
                        orderIdsToIcmIds.Add(id, icmId);
                    }
                    if (!orderIcmIdsToIds.ContainsKey(icmId))
                    {
                        orderIcmIdsToIds.Add(icmId, id);
                    }
                }
                else if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(icmId))
                {
                    orderIcmIdsToIds.TryGetValue(icmId, out id);
                }
                else if (!string.IsNullOrEmpty(id) && string.IsNullOrEmpty(icmId))
                {
                    orderIdsToIcmIds.TryGetValue(id, out icmId);
                }
            }
        }

        private void HandleListStatus(ListStatus listStatus)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleListStatus), string.Empty, $"Handling list status: {listStatus}").Wait();

            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleListStatus), string.Empty, $"Number of orders: {listStatus.TotNoOrders.Obj}").Wait();

            lock (listStatuses)
            {
                var firstTcs = listStatuses.FirstOrDefault(x => !x.Task.IsCompleted);
                firstTcs.SetResult(listStatus);
            }
        }

        private void HandleRejected(Reject reject)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleListStatus), string.Empty, "Handle reject message").Wait();

            switch (reject.RefMsgType.Obj)
            {
                case MsgType.ORDER_STATUS_REQUEST:
                    lock (listStatuses)
                    {
                        var firstTcs = listStatuses.FirstOrDefault(x => !x.Task.IsCompleted);
                        firstTcs.SetException(new OperationRejectedException(reject.Text.Obj));
                    }
                    break;
                case MsgType.LIST_STATUS:
                    break;
            }
        }

        public bool SendAllOrdersStatusRequest()
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(SendAllOrdersStatusRequest), string.Empty, "Trying to send Order Status Request").Wait();

            var request = new OrderStatusRequest();

            request.SetField(new ClOrdID("OPEN_ORDER"));
            request.SetField(new OrderID("OPEN_ORDER"));
            request.SetField(new TransactTime(DateTime.UtcNow));
            request.SetField(new Side('7')); // 7 for any type
            request.SetField(new Symbol("EUR/USDm")); // Tag is required but will be ignored
            request.SetField(new CharField(7559, 'Y')); // custom ICM tag for requesting all open orders

            return SendRequest(request);
        }

        public bool SendOrderStatusRequest(Instrument instrument, string orderId)
        {
            if (!orderSignals.TryGetValue(orderId, out var signal))
            {
                throw new InvalidOperationException($"Can't find signal for order {orderId}");
            }

            logger.WriteInfoAsync(nameof(IcmConnector), nameof(SendAllOrdersStatusRequest), string.Empty, $"Sending order status request for order {orderId}").Wait();

            var request = new OrderStatusRequest(
                new ClOrdID(orderId),
                new Symbol(symbolsMap[instrument.Name]),
                ConvertSide(signal.TradeType));

            request.SetField(new TransactTime(DateTime.UtcNow));
            request.SetField(ConvertType(signal.OrderType));

            lock (orderIdsSyncRoot)
            {
                string icmId;
                if (orderIdsToIcmIds.TryGetValue(orderId, out icmId))
                    request.SetField(new OrderID(icmId));
                else
                    throw new InvalidOperationException($"Can't find icm id in orderIds for {orderId}");

            }

            return SendRequest(request);
        }

        public bool SendSecurityListRequest()
        {
            var request = new SecurityListRequest(
                new SecurityReqID(DateTime.Now.Ticks.ToString()),
                new SecurityListRequestType(SecurityListRequestType.SYMBOL));

            return SendRequest(request);
        }

        public void SendLogout()
        {

        }

        public bool SendRequestForPositions()
        {
            var request = new RequestForPositions(
                new PosReqID(DateTime.Now.Ticks.ToString()),
                new PosReqType(PosReqType.POSITIONS),

                aAccount: new Account("account"),
                aAccountType: new AccountType(AccountType.ACCOUNT_IS_CARRIED_ON_CUSTOMER_SIDE_OF_BOOKS),
                aClearingBusinessDate: new ClearingBusinessDate(DateTimeConverter.ConvertDateOnly(DateTime.Today)),
                aTransactTime: new TransactTime(DateTime.Today));

            //var request = new RequestForPositions();

            request.SetField(new NoPartyIDs(1)); // ICM: Number of parties, should equal 1
            request.SetField(new PartyID("FB")); // ICM: FB - Owner userID
            //request.SetField(new PartyIDSource(PartyIDSource.GENERALLY_ACCEPTED_MARKET_PARTICIPANT_IDENTIFIER));
            request.SetField(new PartyRole(PartyRole.CLIENT_ID)); // ICM: 3 - ClientID, Party Role

            //request.SetField(new PosReqID(Guid.NewGuid().ToString()));
            //request.SetField(new PosReqType(PosReqType.POSITIONS));

            request.SetField(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT)); // ICM: Only snapshot mode is supported


            return SendRequest(request);

            // TODO: some unspecified fields are required: 8=FIX.4.49=15935=349=TS56=ICMD50000395d-TRADE34=309552=20170622-10:57:58.12445=3372=AN371=452373=158=Fix::Message::InboundMessage::Node::operator [](): Missing tag10=119
        }

        private bool SendRequest(Message request)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(SendRequest), string.Empty, $"About to send a request {request}").Wait();

            var header = request.Header;
            header.SetField(new SenderCompID(session.SenderCompID));
            header.SetField(new TargetCompID(session.TargetCompID));

            return Session.SendToTarget(request);
        }

        private bool AddOrder(TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            if (!orderSignals.TryAdd(signal.OrderId, signal))
                throw new InvalidOperationException($"Order with ID {signal.OrderId} was sent already");

            logger.WriteInfoAsync(nameof(IcmConnector), nameof(AddOrder), string.Empty, $"Generating request for sending order for instrument {signal.Instrument}").Wait();

            var request = new NewOrderSingle(
                new ClOrdID(signal.OrderId),
                new Symbol(symbolsMap[signal.Instrument.Name]),
                ConvertSide(signal.TradeType),
                new TransactTime(signal.Time),
                ConvertType(signal.OrderType));

            request.SetField(ConvertTimeInForce(signal.TimeInForce));
            request.SetField(new OrderQty(signal.Volume));
            request.SetField(new Price(signal.Price ?? 0));

            return SendRequest(request);
        }

        private QuickFix.Fields.TimeInForce ConvertTimeInForce(TimeInForce timeInForce)
        {
            switch (timeInForce)
            {
                case TimeInForce.GoodTillCancel:
                    return new QuickFix.Fields.TimeInForce(QuickFix.Fields.TimeInForce.GOOD_TILL_CANCEL);
                case TimeInForce.FillOrKill:
                    return new QuickFix.Fields.TimeInForce(QuickFix.Fields.TimeInForce.FILL_OR_KILL);
                default:
                    throw new ArgumentOutOfRangeException(nameof(timeInForce), timeInForce, null);
            }
        }



        public async Task<OrderStatusUpdate> AddOrderAndWaitResponse(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var id = signal.OrderId;
            var tcs = new TaskCompletionSource<OrderStatusUpdate>();

            lock (orderExecutions)
            {
                if (!orderExecutions.ContainsKey(id))
                {
                    orderExecutions.Add(id, tcs);
                }
                else
                {
                    orderExecutions[id] = tcs;
                }
            }

            var signalSended = AddOrder(signal, translatedSignal);

            if (!signalSended)
                return new OrderStatusUpdate(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId, OrderExecutionStatus.Rejected);

            var sw = Stopwatch.StartNew();
            var task = tcs.Task;
            await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);

            OrderStatusUpdate result = null;

            if (task.IsCompleted)
            {
                result = task.Result;

                if (result.Status == OrderExecutionStatus.New)
                {
                    lock (orderExecutions)
                    {
                        // probably the new result is here already

                        if (orderExecutions[id].Task.IsCompleted &&
                            orderExecutions[id].Task.Result.Status == OrderExecutionStatus.New) // thats the old one, let's change
                        {
                            tcs = new TaskCompletionSource<OrderStatusUpdate>();
                            orderExecutions[id] = tcs;
                            task = tcs.Task;
                        }
                        else if (orderExecutions[id].Task.IsCompleted &&
                                 orderExecutions[id].Task.Result.Status != OrderExecutionStatus.New) // that's the new one, let's return
                        {
                            //result = orderExecutions[id].Task.Result;
                            task = orderExecutions[id].Task;
                        }
                    }

                    await Task.WhenAny(task, Task.Delay(timeout - sw.Elapsed)).ConfigureAwait(false);

                    if (task.IsCompleted)
                    {
                        result = task.Result;
                    }
                    else // In case of FillOrKill orders we don't want to wait if ICM is not on working hours. We will send cancel request.
                    {
                        if (signal.TimeInForce == TimeInForce.FillOrKill)
                        {
                            result = await CancelOrderAndWaitResponse(signal, translatedSignal, timeout);
                        }
                    }
                }

                return result;
            }
            else // In case of FillOrKill orders we don't want to wait if ICM is not on working hours. We will send cancel request.
            {
                if (signal.TimeInForce == TimeInForce.FillOrKill)
                {
                    result = await CancelOrderAndWaitResponse(signal, translatedSignal, timeout);
                }
            }

            return result;
        }

        private bool CancelOrder(TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            logger.WriteInfoAsync(nameof(IcmConnector), nameof(CancelOrder), string.Empty, $"Generating request for cancelling order {signal.OrderId} for {signal.Instrument.Name}").Wait();

            var request = new OrderCancelRequest(
                new OrigClOrdID(signal.OrderId.ToString()),
                new ClOrdID(signal.OrderId + "cancel"),
                new Symbol(symbolsMap[signal.Instrument.Name]),
                ConvertSide(signal.TradeType),
                new TransactTime(signal.Time));

            lock (orderIdsSyncRoot)
            {
                string icmId;
                if (orderIdsToIcmIds.TryGetValue(signal.OrderId, out icmId))
                    request.SetField(new OrderID(icmId));
                else
                    throw new InvalidOperationException($"Can't find icm id in orderIds for {signal.OrderId}");
            }

            return SendRequest(request);
        }

        public async Task<OrderStatusUpdate> CancelOrderAndWaitResponse(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var id = signal.OrderId;
            var tcs = new TaskCompletionSource<OrderStatusUpdate>();

            lock (orderExecutions)
            {
                if (orderExecutions.ContainsKey(id))
                {
                    orderExecutions[id] = tcs;
                }
                else
                {
                    orderExecutions.Add(id, tcs);
                }
            }

            var signalSended = CancelOrder(signal, translatedSignal);

            if (!signalSended)
                return new OrderStatusUpdate(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId, OrderExecutionStatus.Rejected);

            await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);

            if (!tcs.Task.IsCompleted)
                throw new InvalidOperationException("Request timed out with no response from ICM");

            if (tcs.Task.IsFaulted)
                throw new InvalidOperationException(tcs.Task.Exception.Message);

            return tcs.Task.Result;
        }

        private readonly Dictionary<string, TaskCompletionSource<OrderStatusUpdate>> orderExecutions = new Dictionary<string, TaskCompletionSource<OrderStatusUpdate>>();

        private readonly ConcurrentDictionary<string, TradingSignal> orderSignals = new ConcurrentDictionary<string, TradingSignal>();


        public Task<OrderStatusUpdate> GetOrderInfoAndWaitResponse(Instrument instrument, string orderId)
        {
            lock (orderExecutions)
            {
                if (orderExecutions.ContainsKey(orderId))
                {
                    if (!orderExecutions[orderId].Task.IsCompleted)
                    {
                        throw new Exception($"Request for {orderId} is in progress");
                    }
                    else
                    {
                        orderExecutions[orderId] = new TaskCompletionSource<OrderStatusUpdate>();
                    }
                }
            }

            SendOrderStatusRequest(instrument, orderId);

            return orderExecutions[orderId].Task;
        }


        private readonly LinkedList<TaskCompletionSource<ListStatus>> listStatuses = new LinkedList<TaskCompletionSource<ListStatus>>();

        public async Task<IEnumerable<OrderStatusUpdate>> GetAllOrdersInfo(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<ListStatus>();

            lock (listStatuses)
            {
                listStatuses.AddLast(tcs);
            }

            var sended = SendAllOrdersStatusRequest();

            if (!sended)
            {
                lock (listStatuses)
                {
                    listStatuses.Remove(tcs);
                }
                return null;
            }

            await Task.WhenAny(tcs.Task, Task.Delay(timeout));

            lock (listStatuses)
            {
                listStatuses.Remove(tcs);

                if (!tcs.Task.IsCompleted)
                {
                    throw new InvalidOperationException("Request timed out with no response from ICM");
                }
            }

            if (tcs.Task.IsFaulted)
            {
                throw new InvalidOperationException(tcs.Task.Exception.Message);
            }

            var listStatus = tcs.Task.Result;
            int ordersCount = listStatus.TotNoOrders.Obj;

            var result = new List<OrderStatusUpdate>();

            for (int i = 1; i <= ordersCount; i++)
            {
                var orderGroup = listStatus.GetGroup(i, Tags.NoOrders);

                var executedTrade = new OrderStatusUpdate(
                    new Instrument("icm", orderGroup.GetField(Tags.Symbol)),
                    orderGroup.GetField(new TransactTime()).Obj,
                    orderGroup.GetField(new AvgPx()).Obj,
                    orderGroup.GetField(new LeavesQty()).Obj,
                    //ConvertType((OrdType)orderGroup.GetField(new OrdType())),
                    ConvertSide(orderGroup.GetField(new Side()).Obj),
                    orderGroup.GetField(new ClOrdID()).Obj,
                    ConvertStatus(orderGroup.GetField(new OrdStatus()).Obj));

                result.Add(executedTrade);
            }

            return result;
        }

        private Side ConvertSide(TradeType tradeType)
        {
            return new Side(tradeType == TradeType.Buy ? Side.BUY : Side.SELL);
        }

        private TradeType ConvertSide(char side)
        {
            return side == Side.BUY ? TradeType.Buy : TradeType.Sell;
        }

        private OrderType ConvertType(OrdType ordType)
        {
            switch (ordType.Obj)
            {
                case OrdType.FOREX_MARKET:
                    return OrderType.Market;
                case OrdType.FOREX_LIMIT:
                    return OrderType.Limit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ordType), ordType.Obj, null);
            }
        }

        private OrderExecutionStatus ConvertStatus(char status)
        {
            switch (status)
            {
                case OrdStatus.NEW:
                    return OrderExecutionStatus.New;
                case OrdStatus.CANCELED:
                    return OrderExecutionStatus.Cancelled;
                case OrdStatus.PENDING_NEW:
                    return OrderExecutionStatus.Pending;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private OrdType ConvertType(OrderType orderType)
        {
            char typeName;

            switch (orderType)
            {
                case OrderType.Market:
                    typeName = OrdType.FOREX_MARKET;
                    break;
                case OrderType.Limit:
                    typeName = OrdType.FOREX_LIMIT;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }

            return new OrdType(typeName);
        }
    }
}
