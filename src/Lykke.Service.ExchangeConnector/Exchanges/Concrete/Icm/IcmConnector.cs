using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using QuickFix.Transport;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Trading;
using TradingBot.Communications;
using TradingBot.Handlers;
using TradingBot.Repositories;
using ExecType = QuickFix.Fields.ExecType;
using ExecutionReport = TradingBot.Trading.ExecutionReport;
using Message = QuickFix.Message;
using TradeType = TradingBot.Trading.TradeType;
using TimeInForce = TradingBot.Trading.TimeInForce;
using ILog = Common.Log.ILog;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal sealed class IcmConnector : IApplication, IIcmConnector
    {
        private readonly ILog _logger;
        private readonly IcmConfig _config;
        private readonly IcmModelConverter _modelConverter;
        private readonly IAzureFixMessagesRepository _repository;
        private readonly IHandler<ExecutionReport> _tradeHandler;

        private SessionID _session;


        private readonly object _orderIdsSyncRoot = new object();
        private readonly Dictionary<string, string> _orderIdsToIcmIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _orderIcmIdsToIds = new Dictionary<string, string>();
        private readonly LinkedList<TaskCompletionSource<ListStatus>> _listStatuses = new LinkedList<TaskCompletionSource<ListStatus>>();
        private readonly SocketInitiator _initiator;

        public event Action Connected;
        public event Action Disconnected;

        public IcmConnector(IcmConfig config, IcmModelConverter modelConverter, IAzureFixMessagesRepository repository, IHandler<ExecutionReport> tradeHandler, ILog logger)
        {
            _config = config;
            _modelConverter = modelConverter;
            _repository = repository;
            _tradeHandler = tradeHandler;
            _logger = logger;

            var settings = new SessionSettings(_config.GetFixConfigAsReader());

            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new LykkeLogFactory(_logger);

            _initiator = new SocketInitiator(this, storeFactory, settings, logFactory);
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            if (message is Logon logon)
            {
                logon.Username = new Username(_config.Username);
                logon.Password = new Password(_config.Password);
            }

            _repository.SaveMessage(message, FixMessageDirection.ToAdmin);
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, $"FromAdmin message: {message}").Wait();

            _repository.SaveMessage(message, FixMessageDirection.FromAdmin);

            try
            {
                switch (message)
                {
                    case Reject reject:
                        HandleRejected(reject);
                        break;
                    case null:
                        _logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, "Received null message").Wait();
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.WriteErrorAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, e).Wait();
            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(ToApp), string.Empty, $"Outgoing (ToApp) message is sent: {message}").Wait();
            _repository.SaveMessage(message, FixMessageDirection.ToApp);
        }

        public void FromApp(Message message, SessionID sessionId)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromAdmin), string.Empty, $"FromApp message: {message}").Wait();
            _repository.SaveMessage(message, FixMessageDirection.FromApp);

            try
            {
                switch (message)
                {
                    case QuickFix.FIX44.ExecutionReport executionReport:
                        HandleExecutionReport(executionReport);
                        break;
                    case SecurityList securityList:
                        //HandleSecurityList(securityList);
                        break;
                    case PositionReport positionReport:
                        HandlePositionReport(positionReport);
                        break;
                    case MarketDataIncrementalRefresh marketDataIncrementalRefresh:
                        _logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromApp), string.Empty, "Market data incremental refresh is not supported. We read the data from RabbitMQ").Wait();
                        break;
                    case ListStatus listStatus:
                        HandleListStatus(listStatus);
                        break;
                    default:
                        break;
                    case null:
                        _logger.WriteInfoAsync(nameof(IcmConnector), nameof(FromApp), string.Empty, "Received null message").Wait();
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.WriteErrorAsync(nameof(IcmConnector), nameof(FromApp), string.Empty, e).Wait();
            }
        }

        public void OnCreate(SessionID sessionID)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(OnCreate), string.Empty, $"Session created {sessionID}").Wait();
        }

        public void OnLogout(SessionID sessionID)
        {
            _session = null;
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(OnLogout), string.Empty, $"Logout session {sessionID}").Wait();

            Disconnected?.Invoke();
        }

        public void OnLogon(SessionID sessionID)
        {
            _session = sessionID;
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(OnLogon), string.Empty, $"Logon for session {sessionID}").Wait();

            SendRequestForPositions();
            //SendSecurityListRequest();
            //SendOrderStatusRequest();

            Connected?.Invoke();
        }

        //        private void HandleSecurityList(SecurityList securityList)
        //        {
        //            var securitiesGruop = new SecurityList.NoRelatedSymGroup();
        //
        //            var sb = new StringBuilder();
        //
        //            for (int i = 1; i <= securityList.NoRelatedSym.Obj; i++)
        //            {
        //                securityList.GetGroup(i, securitiesGruop);
        //
        //                sb.Append($"{securitiesGruop.Symbol}. ");
        //
        //                var attrGroup = new SecurityList.NoRelatedSymGroup.NoInstrAttribGroup();
        //
        //                for (int j = 1; j <= securitiesGruop.NoInstrAttrib.Obj; j++)
        //                {
        //                    securitiesGruop.GetGroup(j, attrGroup);
        //
        //                    if (attrGroup.InstrAttribType.Obj == 18) // Forex Instrument decimal places in ICM
        //                    {
        //                        sb.AppendLine($"Decimal places: {attrGroup.InstrAttribValue}");
        //                    }
        //                }
        //            }
        //
        //            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleSecurityList), string.Empty, sb.ToString()).Wait();
        //        }

        private void HandlePositionReport(PositionReport positionReport)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"Position report received: {positionReport}. Base currency: {positionReport.Currency.Obj}, Symbol: {positionReport.Symbol.Obj}").Wait();


            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"Number of positions: {positionReport.NoPositions.Obj}");

            var positionsGroup = new PositionReport.NoPositionsGroup();

            for (int i = 1; i <= positionReport.NoPositions.Obj; i++)
            {
                positionReport.GetGroup(i, positionsGroup);

                if (positionsGroup.IsSetLongQty())
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"{i}: Long: {positionsGroup.LongQty.Obj}").Wait();
                else if (positionsGroup.IsSetShortQty())
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandlePositionReport), string.Empty, $"{i}: Short: {positionsGroup.ShortQty.Obj}").Wait();
            }
        }


        private readonly Dictionary<string, string> _symbolsMap = new Dictionary<string, string>
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


        private readonly Dictionary<string, string> _symbolsMapBack = new Dictionary<string, string>
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

        private void HandleExecutionReport(QuickFix.FIX44.ExecutionReport report)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, "Execution report was received").Wait();

            var orderExecutionStatus = OrderExecutionStatus.Pending;
            var id = report.OrderID.Obj;

            switch (report.ExecType.Obj)
            {
                case ExecType.REJECTED:
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"ICM rejected the order {id}. Rejection reason is {report.OrdRejReason}").Wait();
                    orderExecutionStatus = OrderExecutionStatus.Rejected;
                    break;

                case ExecType.TRADE:

                    if (report.OrdStatus.Obj == OrdStatus.FILLED)
                    {
                        _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"ICM filled the order {id}! OrdType: {report.OrdStatus.Obj}").Wait();
                        orderExecutionStatus = OrderExecutionStatus.Fill;
                    }
                    else if (report.OrdStatus.Obj == OrdStatus.PARTIALLY_FILLED)
                    {
                        _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"ICM filled the order {id} partially.").Wait();
                        orderExecutionStatus = OrderExecutionStatus.PartialFill;
                    }
                    break;

                case ExecType.NEW:
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Order {id} placed as new!").Wait();
                    orderExecutionStatus = OrderExecutionStatus.New;
                    break;

                case ExecType.CANCELLED:
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Order {id} was canceled!").Wait();
                    orderExecutionStatus = OrderExecutionStatus.Cancelled;
                    break;

                case ExecType.ORDER_STATUS:
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Order status for {id}, side: {report.Side}, orderQty: {report.OrderQty}").Wait();
                    break;

                case ExecType.PENDING_CANCEL:
                    _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleExecutionReport), string.Empty, $"Cancellation of the order {id} is pending!").Wait();
                    break;
            }



            var executedTrade =_modelConverter.ConvertExecutionReport(report, orderExecutionStatus);

            if (orderExecutionStatus == OrderExecutionStatus.Cancelled ||
                orderExecutionStatus == OrderExecutionStatus.Fill ||
                orderExecutionStatus == OrderExecutionStatus.PartialFill)
            {
                _tradeHandler.Handle(executedTrade).GetAwaiter().GetResult();
            }


            lock (_orderExecutions)
            {
                if (_orderExecutions.ContainsKey(id))
                {
                    if (_orderExecutions[id].Task.Status < TaskStatus.RanToCompletion)
                    {
                        _orderExecutions[id].SetResult(executedTrade);
                    }
                    else
                    {
                        _orderExecutions[id] = new TaskCompletionSource<ExecutionReport>();
                        _orderExecutions[id].SetResult(executedTrade);
                    }
                }
            }
        }

        private void HandleListStatus(ListStatus listStatus)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleListStatus), string.Empty, $"Handling list status: {listStatus}").Wait();

            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleListStatus), string.Empty, $"Number of orders: {listStatus.TotNoOrders.Obj}").Wait();

            lock (_listStatuses)
            {
                var firstTcs = _listStatuses.FirstOrDefault(x => !x.Task.IsCompleted);
                firstTcs?.SetResult(listStatus);
            }
        }

        private void HandleRejected(Reject reject)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(HandleListStatus), string.Empty, "Handle reject message").Wait();

            switch (reject.RefMsgType.Obj)
            {
                case MsgType.ORDER_STATUS_REQUEST:
                    lock (_listStatuses)
                    {
                        var firstTcs = _listStatuses.FirstOrDefault(x => !x.Task.IsCompleted);
                        firstTcs?.SetException(new OperationRejectedException(reject.Text.Obj));
                    }
                    break;
                case MsgType.LIST_STATUS:
                    break;
            }
        }

        public bool SendAllOrdersStatusRequest()
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(SendAllOrdersStatusRequest), string.Empty, "Trying to send Order Status Request").Wait();

            var request = new OrderStatusRequest();

            request.SetField(new ClOrdID("OPEN_ORDER"));
            request.SetField(new OrderID("OPEN_ORDER"));
            request.SetField(new TransactTime(DateTime.UtcNow));
            request.SetField(new Side('7')); // 7 for any type
            request.SetField(new Symbol("EUR/USDm")); // Tag is required but will be ignored
            request.SetField(new CharField(7559, 'Y')); // custom ICM tag for requesting all open orders

            return SendRequest(request);
        }

        private bool SendOrderStatusRequest(Instrument instrument, string orderId)
        {
            if (!_orderSignals.TryGetValue(orderId, out var signal))
            {
                throw new InvalidOperationException($"Can't find signal for order {orderId}");
            }

            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(SendAllOrdersStatusRequest), string.Empty, $"Sending order status request for order {orderId}").Wait();

            var request = new OrderStatusRequest(
                new ClOrdID(orderId),
                new Symbol(_symbolsMap[instrument.Name]),
                ConvertSide(signal.TradeType));

            request.SetField(new TransactTime(DateTime.UtcNow));
            request.SetField(ConvertType(signal.OrderType));

            lock (_orderIdsSyncRoot)
            {
                string icmId;
                if (_orderIdsToIcmIds.TryGetValue(orderId, out icmId))
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

        private void SendRequestForPositions()
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

            SendRequest(request);
        }

        private bool SendRequest(Message request)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(SendRequest), string.Empty, $"About to send a request {request}").Wait();

            var header = request.Header;
            header.SetField(new SenderCompID(_session.SenderCompID));
            header.SetField(new TargetCompID(_session.TargetCompID));

            return Session.SendToTarget(request);
        }

        private bool AddOrder(TradingSignal signal)
        {
            if (!_orderSignals.TryAdd(signal.OrderId, signal))
                throw new InvalidOperationException($"Order with ID {signal.OrderId} was sent already");

            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(AddOrder), string.Empty, $"Generating request for sending order for instrument {signal.Instrument}").Wait();

            var request = new NewOrderSingle(
                new ClOrdID(signal.OrderId),
                new Symbol(_symbolsMap[signal.Instrument.Name]),
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



        public async Task<ExecutionReport> AddOrderAndWaitResponse(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var id = signal.OrderId;
            var tcs = new TaskCompletionSource<ExecutionReport>();

            lock (_orderExecutions)
            {
                if (!_orderExecutions.ContainsKey(id))
                {
                    _orderExecutions.Add(id, tcs);
                }
                else
                {
                    _orderExecutions[id] = tcs;
                }
            }

            var signalSended = AddOrder(signal);

            if (!signalSended)
                return new ExecutionReport(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId, OrderExecutionStatus.Rejected);

            var sw = Stopwatch.StartNew();
            var task = tcs.Task;
            await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);

            ExecutionReport result = null;

            if (task.IsCompleted)
            {
                result = task.Result;

                if (result.ExecutionStatus == OrderExecutionStatus.New)
                {
                    lock (_orderExecutions)
                    {
                        // probably the new result is here already

                        if (_orderExecutions[id].Task.IsCompleted &&
                            _orderExecutions[id].Task.Result.ExecutionStatus == OrderExecutionStatus.New) // thats the old one, let's change
                        {
                            tcs = new TaskCompletionSource<ExecutionReport>();
                            _orderExecutions[id] = tcs;
                            task = tcs.Task;
                        }
                        else if (_orderExecutions[id].Task.IsCompleted &&
                                 _orderExecutions[id].Task.Result.ExecutionStatus != OrderExecutionStatus.New) // that's the new one, let's return
                        {
                            //result = orderExecutions[id].Task.Result;
                            task = _orderExecutions[id].Task;
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

            if (signal.TimeInForce == TimeInForce.FillOrKill)
            {
                result = await CancelOrderAndWaitResponse(signal, translatedSignal, timeout);
            }

            return result;
        }

        private bool CancelOrder(TradingSignal signal)
        {
            _logger.WriteInfoAsync(nameof(IcmConnector), nameof(CancelOrder), string.Empty, $"Generating request for canceling order {signal.OrderId} for {signal.Instrument.Name}").Wait();

            var request = new OrderCancelRequest(
                new OrigClOrdID(signal.OrderId),
                new ClOrdID(signal.OrderId + "cancel"),
                new Symbol(_symbolsMap[signal.Instrument.Name]),
                ConvertSide(signal.TradeType),
                new TransactTime(signal.Time));

            lock (_orderIdsSyncRoot)
            {
                string icmId;
                if (_orderIdsToIcmIds.TryGetValue(signal.OrderId, out icmId))
                    request.SetField(new OrderID(icmId));
                else
                    throw new InvalidOperationException($"Can't find icm id in orderIds for {signal.OrderId}");
            }

            return SendRequest(request);
        }

        public async Task<ExecutionReport> CancelOrderAndWaitResponse(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var id = signal.OrderId;
            var tcs = new TaskCompletionSource<ExecutionReport>();

            lock (_orderExecutions)
            {
                if (_orderExecutions.ContainsKey(id))
                {
                    _orderExecutions[id] = tcs;
                }
                else
                {
                    _orderExecutions.Add(id, tcs);
                }
            }

            var signalSended = CancelOrder(signal);

            if (!signalSended)
                return new ExecutionReport(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId, OrderExecutionStatus.Rejected);

            await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);

            if (!tcs.Task.IsCompleted)
                throw new InvalidOperationException("Request timed out with no response from ICM");

            if (tcs.Task.IsFaulted)
                throw new InvalidOperationException(tcs.Task.Exception.Message);

            return tcs.Task.Result;
        }

        private readonly Dictionary<string, TaskCompletionSource<ExecutionReport>> _orderExecutions = new Dictionary<string, TaskCompletionSource<ExecutionReport>>();

        private readonly ConcurrentDictionary<string, TradingSignal> _orderSignals = new ConcurrentDictionary<string, TradingSignal>();


        public Task<ExecutionReport> GetOrderInfoAndWaitResponse(Instrument instrument, string orderId)
        {
            lock (_orderExecutions)
            {
                if (_orderExecutions.ContainsKey(orderId))
                {
                    if (!_orderExecutions[orderId].Task.IsCompleted)
                    {
                        throw new InvalidOperationException($"Request for {orderId} is in progress");
                    }

                    _orderExecutions[orderId] = new TaskCompletionSource<ExecutionReport>();
                }
            }

            SendOrderStatusRequest(instrument, orderId);

            return _orderExecutions[orderId].Task;
        }




        public async Task<IEnumerable<ExecutionReport>> GetAllOrdersInfo(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<ListStatus>();

            lock (_listStatuses)
            {
                _listStatuses.AddLast(tcs);
            }

            var sended = SendAllOrdersStatusRequest();

            if (!sended)
            {
                lock (_listStatuses)
                {
                    _listStatuses.Remove(tcs);
                }
                return null;
            }

            await Task.WhenAny(tcs.Task, Task.Delay(timeout));

            lock (_listStatuses)
            {
                _listStatuses.Remove(tcs);

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

            var result = new List<ExecutionReport>();

            for (int i = 1; i <= ordersCount; i++)
            {
                var orderGroup = listStatus.GetGroup(i, Tags.NoOrders);

                var executedTrade = new ExecutionReport(
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

        public void Start()
        {
            _initiator.Start();
        }

        public void Dispose()
        {
            _initiator.Dispose();
        }

        public void Stop()
        {
            _initiator.Stop();
        }
    }
}
