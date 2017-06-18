using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using TradingBot.Common.Infrastructure;
using TradingBot.FixConnector.Configuration;

namespace TradingBot.FixConnector
{
    public class ICMConnector : IApplication
    {
        private ILogger Logger = Logging.CreateLogger<ICMConnector>();

        private SessionID session;
        private ConnectionConfig config;

        private Dictionary<string, AssetBook> books = new Dictionary<string, AssetBook>();
        private Queue<FullOrderBook> outQueue = new Queue<FullOrderBook>();

        public ICMConnector(ConnectionConfig config)
        {
            this.config = config;
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionID)
        {
            throw new NotSupportedException();
        }

        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
			try
			{
                if (!(message is MarketDataIncrementalRefresh))
                    return;
                
                var noMDEntries = new NoMDEntries();

                //((MarketDataIncrementalRefresh)message).Get(noMDEntries);

                var priceGroup = new MarketDataIncrementalRefresh.NoMDEntriesGroup();

                AssetBook book = null;

                for (int i = 1; i <= noMDEntries.getValue(); i++)
                {
                    message.GetGroup(i, priceGroup);
                    var type = priceGroup.GetField(new MDUpdateAction()).getValue();
                    var asset = priceGroup.GetField(new Symbol()).getValue();
                    book = books[asset];

				    if (type == MDUpdateAction.NEW)
					{
                        var side = priceGroup.GetField(new MDEntryType()).getValue();
                        var id = priceGroup.GetField(new MDEntryID()).getValue();
                        var curPrice = priceGroup.GetField(new MDEntryPx()).getValue();
                        var curVolume = priceGroup.GetField(new MDEntrySize()).getValue();

                        var quote = new Quote(curPrice, curVolume, side == MDEntryType.BID);

                        var oldQuote = book.QuotesMap[id];

						if (oldQuote != null)
						{
							if (quote.IsBuy)
							{
                                book.Bid.Remove(oldQuote);
                                book.Bid.Add(quote);
						    }
							else
							{
                                book.Ask.Remove(oldQuote);
                                book.Ask.Add(quote);
							}
						}
						else
						{
							if (quote.IsBuy)
							{
                                book.Bid.Add(quote);
						    }
							else
							{
                                book.Ask.Add(quote);
						    }
						}
                        book.QuotesMap[id] = quote;
				    }
					else if (type == MDUpdateAction.DELETE)
					{
                        var id = priceGroup.GetField(new MDEntryID()).getValue();

                        if (book.QuotesMap.ContainsKey(id))
                        {
							var quote = book.QuotesMap[id];
							book.QuotesMap.Remove(id);


							if(quote.IsBuy)
							{
                                book.Bid.Remove(quote);
							}
                            else
                            {
                                book.Ask.Remove(quote);
							}
						}
					}
					else
					{
                        Logger.LogDebug($"{type}");
				    }
				}
				if (book != null)
				{
                    var now = DateTime.UtcNow;

                    var bids = new List<PriceVolume>(
                        book.Bid.OrderBy(x => x.Price).Select(x => new PriceVolume(x.Price, x.Volume)));
                    
                    var asks = new List<PriceVolume>(
                        book.Ask.OrderBy(x => x.Price).Select(x => new PriceVolume(x.Price, x.Volume)));
                    
                    outQueue.Enqueue(new FullOrderBook(config.Name, book.Asset, now, asks, bids));
			    }
			}
			catch (Exception e) 
            {
                Logger.LogError(new EventId(), e, "Error");
		    }
        }

        public void OnCreate(SessionID sessionID)
        {
            Logger.LogInformation($"Session created {sessionID}");
        }

        public void OnLogon(SessionID sessionID)
        {
            session = sessionID;
            Logger.LogInformation($"Logon for session {sessionID}");
            // TODO: subscribe to assets
        }

        public void OnLogout(SessionID sessionID)
        {
            session = null;
            Logger.LogInformation($"Logout session {sessionID}");
        }

        public void ToAdmin(QuickFix.Message message, SessionID sessionID)
        {
            var header = message.Header;

		    try
			{
				if (header.GetString(0 /*TODO: MsgType.FIELD*/) == MsgType.LOGON)
				{
                    message.SetField(new Username(config.Username));
                    message.SetField(new Password(config.Password));
				}
			}
			catch (FieldNotFoundException fieldNotFound) 
            {
                //throw new IllegalStateException(fieldNotFound);
                throw fieldNotFound;
		    }
        }

        public void ToApp(QuickFix.Message message, SessionID sessionId)
        {
            throw new NotSupportedException();
        }


		private void SubscribeToAssets()
		{
            var mDReqID = new MDReqID("MDRQ-" + DateTime.UtcNow.Ticks);
            var subscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES);
            var marketDepth = new MarketDepth(0);
            var marketDataRequest = new MarketDataRequest(mDReqID, subscriptionRequestType, marketDepth);

            marketDataRequest.Set(new MDUpdateType(MDUpdateType.INCREMENTAL_REFRESH));
            marketDataRequest.Set(new AggregatedBook(true));

            var mDEntryTypesGroup = new MarketDataRequest.NoMDEntryTypesGroup();
            MarketDataRequest.NoRelatedSymGroup symGroup;

            foreach (var instrument in config.Instruments.Values)
            {
                mDEntryTypesGroup.Set(new MDEntryType(MDEntryType.BID));
                marketDataRequest.AddGroup(mDEntryTypesGroup);

                mDEntryTypesGroup.Set(new MDEntryType(MDEntryType.OFFER));
                marketDataRequest.AddGroup(mDEntryTypesGroup);

                symGroup = new MarketDataRequest.NoRelatedSymGroup();
                symGroup.SetField(new Symbol(instrument));
                marketDataRequest.AddGroup(symGroup);
			}

            var header = marketDataRequest.Header;
            header.SetField(new BeginString("FIX.4.4"));
            header.SetField(new SenderCompID(session.SenderCompID));
            header.SetField(new TargetCompID(session.TargetCompID));

            Session.SendToTarget(marketDataRequest);
		}
    }
}
