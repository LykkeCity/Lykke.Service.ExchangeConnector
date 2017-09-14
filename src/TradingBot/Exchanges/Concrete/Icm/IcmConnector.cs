using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;
using TradingBot.Communications;
using Message = QuickFix.Message;
using TradeType = TradingBot.Trading.TradeType;
using TimeInForce = TradingBot.Trading.TimeInForce;

namespace TradingBot.Exchanges.Concrete.Icm
{
    public class IcmConnector : IApplication
    {
        private readonly ILogger logger = Logging.CreateLogger<IcmConnector>();
        private readonly IcmConfig config;
        private readonly AzureFixMessagesRepository repository;

        private SessionID session;
        
        
        private readonly object orderIdsSyncRoot = new Object();
        private readonly Dictionary<string, string> orderIdsToIcmIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> orderIcmIdsToIds = new Dictionary<string, string>();

        public event Action Connected;
        public event Action Disconnected;
        public event Func<ExecutedTrade, Task> OnTradeExecuted;

        public IcmConnector(IcmConfig config, AzureFixMessagesRepository repository)
        {
            this.config = config;
            this.repository = repository;
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
            logger.LogInformation($"FromAdmin message: {message}");
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
                        logger.LogInformation("Received null message");
                        break;
                }
            }
            catch (Exception e) 
            {
                logger.LogError(0, e, "Error");
            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            logger.LogInformation($"Outgoing (ToApp) message is sent: {message}");
            repository.SaveMessage(message, FixMessageDirection.ToApp);
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            logger.LogInformation($"FromApp message: {message}");
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
                        logger.LogInformation("Market data incremental refresh is not supported. We read the data from RabbitMQ");
                        break;
                    case ListStatus listStatus:
                        HandleListStatus(listStatus);
                        break;
                    default:
                        break;
                    case null:
                        logger.LogInformation("Received null message");
                        break;
                }
            }
            catch (Exception e) 
            {
                logger.LogError(0, e, "Error");
            }
        }

        public void OnCreate(SessionID sessionID)
        {
            logger.LogInformation($"Session created {sessionID}");
        }

        public void OnLogout(SessionID sessionID)
        {
            session = null;
            logger.LogInformation($"Logout session {sessionID}");
            
            Disconnected?.Invoke();
        }

        public void OnLogon(SessionID sessionID)
        {
            session = sessionID;
            logger.LogInformation($"Logon for session {sessionID}");

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
            
            logger.LogInformation(sb.ToString());
        }

        private void HandlePositionReport(PositionReport positionReport)
        {
            logger.LogInformation($"Position report received: {positionReport}. Base currency: {positionReport.Currency.Obj}, Symbol: {positionReport.Symbol.Obj}");
            
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
            
            
            logger.LogInformation($"Number of positions: {positionReport.NoPositions.Obj}");
            
            var positionsGroup = new PositionReport.NoPositionsGroup();
            
            for (int i = 1; i <= positionReport.NoPositions.Obj; i++)
            {
                positionReport.GetGroup(i, positionsGroup);
                
                if (positionsGroup.IsSetLongQty())
                    logger.LogInformation($"{i}: Long: {positionsGroup.LongQty.Obj}");
                else if (positionsGroup.IsSetShortQty())
                    logger.LogInformation($"{i}: Short: {positionsGroup.ShortQty.Obj}");
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
            logger.LogDebug("Execution report was received");

            string id;
            string icmId;
            GetIds(report, out id, out icmId);
            
            ExecutionStatus executionStatus = ExecutionStatus.Pending;

            switch (report.ExecType.Obj)
            {
                case ExecType.REJECTED:
                    logger.LogInformation($"ICM rejected the order {id}. Rejection reason is {report.OrdRejReason}");
                    executionStatus = ExecutionStatus.Rejected;
                    break;
                    
                case ExecType.TRADE:

                    if (report.OrdStatus.Obj == OrdStatus.FILLED)
                    {
                        logger.LogInformation($"ICM filled the order {id}! OrdType: {report.OrdStatus.Obj}");
                        executionStatus = ExecutionStatus.Fill;    
                    }
                    else if (report.OrdStatus.Obj == OrdStatus.PARTIALLY_FILLED)
                    {
                        logger.LogInformation($"ICM filled the order {id} partially.");
                        executionStatus = ExecutionStatus.PartialFill;
                    }
                    break;
                    
                case ExecType.NEW:
                    logger.LogInformation($"Order {id} placed as new!");
                    executionStatus = ExecutionStatus.New;
                    break;
                    
                case ExecType.CANCELLED:
                    logger.LogInformation($"Order {id} was cancelled!");
                    executionStatus = ExecutionStatus.Cancelled;
                    break;
                    
                case ExecType.ORDER_STATUS:
                    logger.LogInformation($"Order status for {id}, side: {report.Side}, orderQty: {report.OrderQty}");
                    break;
                    
                case ExecType.PENDING_CANCEL:
                    logger.LogInformation($"Cancellation of the order {id} is pending!");
                    break;
            }

            
            
            var executedTrade = new ExecutedTrade(
                new Instrument(IcmExchange.Name, report.IsSetField(Tags.Symbol) ? symbolsMapBack[report.Symbol.Obj] : ""),    
                report.IsSetField(Tags.TransactTime) ? report.TransactTime.Obj : DateTime.UtcNow,
                executionStatus == ExecutionStatus.Fill || executionStatus == ExecutionStatus.PartialFill ? report.AvgPx.Obj : report.Price.Obj,
                report.OrderQty.Obj,
                report.Side.Obj == Side.BUY ? TradeType.Buy : TradeType.Sell,
                id,
                executionStatus);

            if (report.IsSetField(Tags.Text))
            {
                executedTrade.Message = report.Text.Obj;
            }
            
            if (executionStatus == ExecutionStatus.Cancelled ||
                executionStatus == ExecutionStatus.Fill ||
                executionStatus == ExecutionStatus.PartialFill)
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
                        orderExecutions[id] = new TaskCompletionSource<ExecutedTrade>();
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
            logger.LogDebug($"Handling list status: {listStatus}");
            
            logger.LogDebug($"Number of orders: {listStatus.TotNoOrders.Obj}");

            lock (listStatuses)
            {
                var firstTcs = listStatuses.FirstOrDefault(x => !x.Task.IsCompleted);
                firstTcs.SetResult(listStatus);
            }
        }

        private void HandleRejected(Reject reject)
        {
            logger.LogDebug("Handle reject message");

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
            logger.LogDebug($"Trying to send Order Status Request");
            
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
            TradingSignal signal;
            if (!orderSignals.TryGetValue(orderId, out signal))
                throw new InvalidOperationException($"Can't find signal for order {orderId}");
            
            logger.LogDebug($"Sending order status request for order {orderId}");
            
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
            logger.LogDebug($"About to send a request {request}");
            
            var header = request.Header;
            header.SetField(new SenderCompID(session.SenderCompID));
            header.SetField(new TargetCompID(session.TargetCompID));

            return Session.SendToTarget(request);
        }

        public bool AddOrder(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            if (!orderSignals.TryAdd(signal.OrderId, signal))
                throw new InvalidOperationException($"Order with ID {signal.OrderId} was sent already");
            
            logger.LogInformation($"Generarting request for sending order for instrument {instrument}");

            var request = new NewOrderSingle(
                new ClOrdID(signal.OrderId),
                new Symbol(symbolsMap[instrument.Name]),
                ConvertSide(signal.TradeType),
                new TransactTime(signal.Time),
                ConvertType(signal.OrderType));
            
            request.SetField(ConvertTimeInForce(signal.TimeInForce));
            request.SetField(new OrderQty(signal.Volume));
            request.SetField(new Price(signal.Price));

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

        public async Task<ExecutedTrade> AddOrderAndWaitResponse(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var id = signal.OrderId;
            var tcs = new TaskCompletionSource<ExecutedTrade>();

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
            
            var signalSended = AddOrder(instrument, signal, translatedSignal);

            if (!signalSended)
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType, signal.OrderId, ExecutionStatus.Rejected);

            var sw = Stopwatch.StartNew();
            var task = tcs.Task;
            await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);

            ExecutedTrade result = null;
            
            if (task.IsCompleted)
            {
                result = task.Result;
                
                if (result.Status == ExecutionStatus.New)
                {
                    lock (orderExecutions)
                    {
                        // probably the new result is here already

                        if (orderExecutions[id].Task.IsCompleted &&
                            orderExecutions[id].Task.Result.Status == ExecutionStatus.New) // thats the old one, let's change
                        {
                            tcs = new TaskCompletionSource<ExecutedTrade>();
                            orderExecutions[id] = tcs;
                            task = tcs.Task;
                        }
                        else if (orderExecutions[id].Task.IsCompleted &&
                                 orderExecutions[id].Task.Result.Status != ExecutionStatus.New) // that's the new one, let's return
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
                            result = await CancelOrderAndWaitResponse(instrument, signal, translatedSignal, timeout);
                        }
                    }
                }
            
                return result;
            }
            else // In case of FillOrKill orders we don't want to wait if ICM is not on working hours. We will send cancel request.
            {
                if (signal.TimeInForce == TimeInForce.FillOrKill)
                {
                    result = await CancelOrderAndWaitResponse(instrument, signal, translatedSignal, timeout);
                }
            }

            return result;
        }

        public bool CancelOrder(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            logger.LogDebug($"Generating request for cancelling order {signal.OrderId} for {instrument}");
            
            var request = new OrderCancelRequest(
                new OrigClOrdID(signal.OrderId.ToString()), 
                new ClOrdID(signal.OrderId + "cancel"), 
                new Symbol(symbolsMap[instrument.Name]), 
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
        
        public async Task<ExecutedTrade> CancelOrderAndWaitResponse(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var id = signal.OrderId;
            var tcs = new TaskCompletionSource<ExecutedTrade>();

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
            
            var signalSended = CancelOrder(instrument, signal, translatedSignal);

            if (!signalSended)
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType, signal.OrderId, ExecutionStatus.Rejected);

            await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);

            if (!tcs.Task.IsCompleted)
                throw new InvalidOperationException("Request timed out with no response from ICM");
            
            if (tcs.Task.IsFaulted)
                throw new InvalidOperationException(tcs.Task.Exception.Message);
            
            return tcs.Task.Result;
        }
        
        private readonly Dictionary<string, TaskCompletionSource<ExecutedTrade>> orderExecutions = new Dictionary<string, TaskCompletionSource<ExecutedTrade>>();
        
        private readonly ConcurrentDictionary<string, TradingSignal> orderSignals = new ConcurrentDictionary<string, TradingSignal>();
        
        
        public Task<ExecutedTrade> GetOrderInfoAndWaitResponse(Instrument instrument, string orderId)
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
                        orderExecutions[orderId] = new TaskCompletionSource<ExecutedTrade>();
                    }
                }
            }
            
            SendOrderStatusRequest(instrument, orderId);

            return orderExecutions[orderId].Task;
        }

        
        private readonly LinkedList<TaskCompletionSource<ListStatus>> listStatuses = new LinkedList<TaskCompletionSource<ListStatus>>();
        
        public async Task<IEnumerable<ExecutedTrade>> GetAllOrdersInfo(TimeSpan timeout)
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
            
            var result = new List<ExecutedTrade>();
            
            for (int i = 1; i <= ordersCount; i++)
            {
                var orderGroup = listStatus.GetGroup(i, Tags.NoOrders);
                
                var executedTrade = new ExecutedTrade(
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

        private ExecutionStatus ConvertStatus(char status)
        {
            switch (status)
            {
                case OrdStatus.NEW:
                    return ExecutionStatus.New;
                case OrdStatus.CANCELED:
                    return ExecutionStatus.Cancelled;
                case OrdStatus.PENDING_NEW:
                    return ExecutionStatus.Pending;
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