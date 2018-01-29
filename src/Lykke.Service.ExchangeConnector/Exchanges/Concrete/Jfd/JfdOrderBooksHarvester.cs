using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.Fields;
using QuickFix.FIX44;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Message = QuickFix.Message;

namespace TradingBot.Exchanges.Concrete.Jfd
{
    internal sealed class JfdOrderBooksHarvester : OrderBooksWebSocketHarvester<MarketDataRequest, Message>
    {
        private readonly IExchangeConfiguration _configuration;
        private readonly JfdModelConverter _modelConverter;

        public JfdOrderBooksHarvester(
            JfdExchangeConfiguration configuration,
            JfdModelConverter modelConverter,
            ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository,
            OrderBookEventsRepository orderBookEventsRepository,
            IHandler<OrderBook> orderBookHandler)
            : base(JfdExchange.Name, configuration, new JfdQuotesSessionConnector(GetConnectorConfig(configuration), log), 
                  log, orderBookSnapshotsRepository, orderBookEventsRepository, orderBookHandler)
        {
            _configuration = configuration;
            _modelConverter = modelConverter;
            HeartBeatPeriod = Timeout.InfiniteTimeSpan; // FIX has its own heartbeat mechanism
        }

        private static FixConnectorConfiguration GetConnectorConfig(JfdExchangeConfiguration exchangeConfiguration)
        {
            return new FixConnectorConfiguration(exchangeConfiguration.Password, exchangeConfiguration.GetQuotingFixConfigAsReader());
        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await Messenger.ConnectAsync(CancellationToken);
                await Subscribe();
                RechargeHeartbeat();


                while (!CancellationToken.IsCancellationRequested)
                {
                    dynamic response = await Messenger.GetResponseAsync(CancellationToken);
                    RechargeHeartbeat();
                    await HandleTableResponse(response);
                }
            }
            finally
            {
                try
                {
                    await Messenger.StopAsync(CancellationToken);
                }
                catch (Exception)
                {
                    // Nothing can do here 
                }
            }
        }

        private async Task HandleTableResponse(TestRequest heartbeat)
        {
            await Log.WriteInfoAsync(nameof(HandleTableResponse), "Heartbeat received", heartbeat.TestReqID.Obj);
        }


        private async Task HandleTableResponse(MarketDataSnapshotFullRefresh snapshot)
        {
            var symbol = snapshot.Symbol.Obj;

            var equFun = new Func<OrderBookItem, OrderBookItem, bool>((item1, item2) => item1.Id == item2.Id);
            var hashFunc = new Func<OrderBookItem, int>(item => item.Id.GetHashCode());
            var orders = new List<OrderBookItem>();
            for (var i = 0; i < snapshot.NoMDEntries.Obj; i++)
            {
                for (var j = 1; j <= snapshot.NoMDEntries.Obj; j++)
                {
                    var ob = snapshot.GetGroup(j, new MarketDataSnapshotFullRefresh.NoMDEntriesGroup());
                    var dir = ob.GetField(new MDEntryType()).Obj;
                    var price = ob.GetField(new MDEntryPx()).Obj;
                    var size = ob.GetField(new MDEntrySize()).Obj;
                    var id = long.Parse(ob.GetField(new QuoteEntryID()).Obj);

                    var ordeItem = new OrderBookItem(equFun, hashFunc)
                    {
                        Id = id.ToString(),
                        Symbol = symbol,
                        IsBuy = dir == MDEntryType.BID,
                        Price = price,
                        Size = size
                    };
                    orders.Add(ordeItem);
                }
            }

            await HandleOrderBookSnapshotAsync(symbol, DateTime.UtcNow, orders);
        }


        private async Task Subscribe()
        {
            var request = new MarketDataRequest
            {
                MDReqID = new MDReqID(DateTime.UtcNow.Ticks.ToString()),
                SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
                MarketDepth = new MarketDepth(50),
                MDUpdateType = new MDUpdateType(MDUpdateType.FULL_REFRESH),
                NoMDEntryTypes = new NoMDEntryTypes(2),
                NoRelatedSym = new NoRelatedSym(_configuration.SupportedCurrencySymbols.Count)
            };
            var bidGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.BID)
            };
            var askGroup = new MarketDataRequest.NoMDEntryTypesGroup
            {
                MDEntryType = new MDEntryType(MDEntryType.OFFER)
            };
            foreach (var mapping in _configuration.SupportedCurrencySymbols)
            {
                var noRelatedSymGroup = new MarketDataRequest.NoRelatedSymGroup
                {
                    Symbol = _modelConverter.ConvertLykkeSymbol(mapping.LykkeSymbol)
                };
                request.AddGroup(noRelatedSymGroup);
            }

            request.AddGroup(bidGroup);
            request.AddGroup(askGroup);
            await Messenger.SendRequestAsync(request, CancellationToken);
        }
    }
}
