using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using TradingBot.Common.Trading;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Logging;
using Message = QuickFix.Message;
using TradeType = TradingBot.Common.Trading.TradeType;

namespace TradingBot.Exchanges.Concrete.ICMarkets
{
    public class IcmConnector : IApplication
    {
        private readonly ILogger logger = Logging.CreateLogger<IcmConnector>();
        private readonly IcmConfig config;

        private SessionID session;

        public IcmConnector(IcmConfig config)
        {
            this.config = config;
        }

        public event Func<ExecutedTrade, Task> OnTradeExecuted;
        
        public void ToAdmin(Message message, SessionID sessionID)
        {            
            var header = message.Header;

            if (header.GetString(Tags.MsgType) == MsgType.LOGON)
            {
                message.SetField(new Username(config.Username));
                message.SetField(new Password(config.Password));
            }
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            logger.LogInformation($"FromAdmin message: {message}");
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            logger.LogInformation($"Outgoing (ToApp) message is sent: {message}");
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            logger.LogInformation($"FromApp message: {message}");	  
            
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
                        logger.LogError("Market data incremental refresh is not supported. We read the data from RabbitMQ");
                        break;
                    case ListStatus listStatus:
                        HandleListStatus(listStatus);
                        break;
                    default:
                        break;
                    case null:
                        logger.LogError("Received null message");
                        break;
                }
            }
            catch (Exception e) 
            {
                logger.LogError(new EventId(), e, "Error");
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
        }

        public void OnLogon(SessionID sessionID)
        {
            session = sessionID;
            logger.LogInformation($"Logon for session {sessionID}");

            SendRequestForPositions();
            SendSecurityListRequest();
            SendOrderStatusRequest();
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

            ExecutionStatus executionStatus = ExecutionStatus.Pending;

            
            switch (report.ExecType.Obj)
            {
                case ExecType.REJECTED:
                    logger.LogError($"ICM rejected the order {report.ClOrdID}. Rejection reason is {report.OrdRejReason}");
                    executionStatus = ExecutionStatus.Rejected;
                    break;
                    
                case ExecType.TRADE:

                    if (report.OrdStatus.Obj == OrdStatus.FILLED)
                    {
                        logger.LogInformation($"ICM filled the order {report.ClOrdID}! OrdType: {report.OrdStatus.Obj}");
                        executionStatus = ExecutionStatus.Fill;    
                    }
                    else if (report.OrdStatus.Obj == OrdStatus.PARTIALLY_FILLED)
                    {
                        logger.LogInformation($"ICM filled the order {report.ClOrdID} partially.");
                        executionStatus = ExecutionStatus.PartialFill;
                    }
                    break;
                    
                case ExecType.NEW:
                    logger.LogInformation($"Order {report.ClOrdID} placed as new!");
                    executionStatus = ExecutionStatus.New;
                    break;
                    
                case ExecType.CANCELLED:
                    logger.LogInformation($"Order {report.ClOrdID} was cancelled!");
                    executionStatus = ExecutionStatus.Cancelled;
                    break;
                    
                case ExecType.ORDER_STATUS:
                    logger.LogInformation($"Order status for {report.ClOrdID}: symbol: {report.Symbol}, side: {report.Side}, orderQty: {report.OrderQty}");
                    break;
                    
                case ExecType.PENDING_CANCEL:
                    logger.LogInformation($"Cancellation of the order {report.ClOrdID} is pending!");
                    break;
            }

            var executedTrade = new ExecutedTrade(
                new Instrument(ICMarketsExchange.Name, symbolsMapBack[report.Symbol.Obj]),
                report.TransactTime.Obj,
                report.Price.Obj,
                report.OrderQty.Obj,
                report.Side.Obj == Side.BUY ? TradeType.Buy : TradeType.Sell,
                long.Parse(report.ClOrdID.Obj),
                executionStatus);

            if (executionStatus == ExecutionStatus.Cancelled ||
                executionStatus == ExecutionStatus.Fill ||
                executionStatus == ExecutionStatus.PartialFill)
            {
                OnTradeExecuted?.Invoke(executedTrade); // TODO: await?
            }

            lock (orderResponses)
            {
                long id = long.Parse(report.ClOrdID.Obj);
                if (orderResponses.ContainsKey(id))
                {
                    // If order is Market Order then wait for Fill or PartialFill, don't send New
                    if (orderResponses[id].Item1.OrderType == OrderType.Market &&
                        (executionStatus == ExecutionStatus.Fill || executionStatus == ExecutionStatus.PartialFill))
                    
                    orderResponses[id].Item2.SetResult(executedTrade);
                }
            }
        }

        public bool SendOrderStatusRequest()
        {
            logger.LogDebug($"Trying to send Order Status Request");
            
            var request = new OrderStatusRequest(
                new ClOrdID("OPEN_ORDER"),
                new Symbol("EUR/USDm"),
                new Side('7')// 7 for any type
            );

            request.SetField(new OrderID("OPEN_ORDER"));
            request.SetField(new TransactTime(DateTime.UtcNow));
            request.SetField(new CharField(7559, 'Y')); // custom ICM tag for requesting all open orders
            //request.SetField(new OrdStatusReqID((requestId = ++orderStatusRequestId).ToString()));

            if (waitedMessages.ContainsKey(11))
                waitedMessages.Remove(11);
            
            waitedMessages.Add(11, null); // TODO: get rid of magic number
            
            return SendRequest(request);
        }
        
        public bool SendSecurityListRequest()
        {
            var request = new SecurityListRequest(
                new SecurityReqID(DateTime.Now.Ticks.ToString()), 
                new SecurityListRequestType(SecurityListRequestType.SYMBOL));
		 
            return SendRequest(request);
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

        public bool AddOrder(Instrument instrument, TradingSignal signal)
        {
            logger.LogInformation($"Generarting request for sending order for instrument {instrument}");

            var request = new NewOrderSingle(
                new ClOrdID(signal.OrderId.ToString()),
                new Symbol(symbolsMap[instrument.Name]),
                ConvertSide(signal.TradeType),
                new TransactTime(signal.Time),
                ConvertType(signal.OrderType));
            
            request.SetField(new TimeInForce(TimeInForce.GOOD_TILL_CANCEL));
            request.SetField(new OrderQty(signal.Count));
            request.SetField(new Price(signal.Price));
            
            return SendRequest(request);
        }

        public bool CancelOrder(Instrument instrument, TradingSignal signal)
        {
            logger.LogDebug($"Generating request for cancelling order {signal.OrderId} for {instrument}");
            
            var request = new OrderCancelRequest(
                new OrigClOrdID(signal.OrderId.ToString()), 
                new ClOrdID(signal.OrderId + "c"), 
                new Symbol(symbolsMap[instrument.Name]), 
                ConvertSide(signal.TradeType),
                new TransactTime(signal.Time)
                );

            return SendRequest(request);
        }

        public void HandleListStatus(ListStatus listStatus)
        {
            logger.LogDebug($"Handling list status: {listStatus}");
            
            logger.LogDebug($"Number of orders: {listStatus.TotNoOrders.Obj}");

            //string requestIdStr = listStatus.GetField(Tags.OrdStatusReqID);
            
            // todo: if this request is awaited should call awaiter. fill response (value) in the dictionary if the key is in it

            if (waitedMessages.ContainsKey(11))
                waitedMessages[11] = listStatus;
        }

        private readonly Dictionary<long, Tuple<TradingSignal, TaskCompletionSource<ExecutedTrade>>> orderResponses = 
            new Dictionary<long, Tuple<TradingSignal, TaskCompletionSource<ExecutedTrade>>>();
        
        public async Task<ExecutedTrade> AddOrderAndWait(Instrument instrument, TradingSignal signal, TimeSpan timeout)
        {
            var id = signal.OrderId;

            var tcs = new TaskCompletionSource<ExecutedTrade>();

            var tuple = Tuple.Create(signal, tcs);
            
            lock (orderResponses)
            {
                orderResponses.Add(id, tuple);   
            }

            var signalSended = AddOrder(instrument, signal);

            if (!signalSended)
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Count, signal.TradeType, signal.OrderId, ExecutionStatus.Rejected);

            ExecutedTrade result = await tcs.Task.ConfigureAwait(false);

            lock (orderResponses)
            {
                orderResponses.Remove(id);
            }
            
            return result;
        }

        public async Task<int> WaitOrderStatusRequest()
        {
            var requestId = 11;
            
            if (!waitedMessages.ContainsKey(requestId)) return 0;
            
            while (waitedMessages[requestId] == null)
            {
                await Task.Delay(100);   
            }

            
            int result = (waitedMessages[requestId] as ListStatus)?.TotNoOrders.Obj ?? 0;

            waitedMessages.Remove(requestId);
            
            return result;
        }

        private Dictionary<long, Message> waitedMessages = new Dictionary<long, Message>();

        private Side ConvertSide(TradeType tradeType)
        {
            return new Side(tradeType == TradeType.Buy ? Side.BUY : Side.SELL);
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