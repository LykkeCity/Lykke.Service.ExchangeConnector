using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.AutorestClient;
using TradingBot.Exchanges.Concrete.AutorestClient.Models;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;
using Instrument = TradingBot.Trading.Instrument;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;
using Position = TradingBot.Exchanges.Concrete.AutorestClient.Models.Position;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexExchange : Exchange
    {
        private readonly BitMexOrderBooksHarvester _orderBooksHarvester;
        private readonly BitmexSocketSubscriber _socketSubscriber;
        private readonly IBitMEXAPI _exchangeApi;
        private readonly BitMexModelConverter _mapper;
        private Timer _measureTimer;
        public new const string Name = "bitmex";

        public BitMexExchange(BitMexExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository,
            BitMexOrderBooksHarvester orderBooksHarvester, ILog log)
            : base(Name, configuration, translatedSignalsRepository, log)
        {
            _mapper = new BitMexModelConverter(configuration.SupportedCurrencySymbols, Name);
            _orderBooksHarvester = orderBooksHarvester;
            _socketSubscriber = new BitmexSocketSubscriber(configuration, log)
                .Subscribe(BitmexTopic.Order, HandleOrder)
                .Subscribe(BitmexTopic.Quote, HandleQuote)
                .Subscribe(BitmexTopic.OrderBookL2, HandleOrderbook);

            var credenitals = new BitMexServiceClientCredentials(configuration.ApiKey, configuration.ApiSecret);
            _exchangeApi = new BitMEXAPI(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl)
            };

            orderBooksHarvester.AddHandler(CallOrderBookHandlers);
            orderBooksHarvester.MaxOrderBookRate = configuration.MaxOrderBookRate;
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
          //  var symbol = BitMexModelConverter.ConvertSymbolFromLykkeToBitMex(instrument.Name, _configuration);
            var symbol = "XBTUSD"; //HACK Hard code!
            var volume = BitMexModelConverter.ConvertVolume(signal.Volume);
            var orderType = BitMexModelConverter.ConvertOrderType(signal.OrderType);
            var side = BitMexModelConverter.ConvertTradeType(signal.TradeType);
            var price = (double?)signal.Price;
            var ct = new CancellationTokenSource(timeout);


            var response = await _exchangeApi.OrdernewAsync(symbol, orderQty: volume, price: price, ordType: orderType, side: side, cancellationToken: ct.Token);

            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }

            var order = (Order)response;

            var execStatus = BitMexModelConverter.ConvertExecutionStatus(order.OrdStatus);
            var execPrice = (decimal)(order.Price ?? 0d);
            var execVolume = (decimal)(order.OrderQty ?? 0d);
            var exceTime = order.TransactTime ?? DateTime.UtcNow;
            var execType = BitMexModelConverter.ConvertTradeType(order.Side);

            translatedSignal.ExternalId = order.OrderID;

            return new ExecutedTrade(signal.Instrument, exceTime, execPrice, execVolume, execType, order.OrderID, execStatus) { Message = order.Text };
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var ct = new CancellationTokenSource(timeout);
            var id = signal.OrderId;
            var response = await _exchangeApi.OrdercancelAsync(cancellationToken: ct.Token, orderID: id);

            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }
            var res = EnsureCorrectResponse(id, response);
            return BitMexModelConverter.OrderToTrade(res[0]);
        }

        public override async Task<ExecutedTrade> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            var filterObj = new { orderID = id };
            var filterArg = JsonConvert.SerializeObject(filterObj);
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.OrdergetOrdersAsync(filter: filterArg, cancellationToken: cts.Token);
            var res = EnsureCorrectResponse(id, response);
            return BitMexModelConverter.OrderToTrade(res[0]);
        }

        public override async Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            var filterObj = new { ordStatus = "New" };
            var filterArg = JsonConvert.SerializeObject(filterObj);
            var response = await _exchangeApi.OrdergetOrdersAsync(filter: filterArg);
            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }

            var trades = ((IReadOnlyCollection<Order>)response).Select( 
                BitMexModelConverter.OrderToTrade);
            return trades;
        }

        public override async Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.UsergetMarginWithHttpMessagesAsync(cancellationToken: cts.Token);
            var bitmexMargin = response.Body;

            var model = BitMexModelConverter.ExchangeBalanceToModel(bitmexMargin);
            return new[] { model };
        }

        public override async Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            var onlyOpensFileter = "{\"isOpen\":true}";
            var response = await _exchangeApi.PositiongetAsync(cancellationToken: cts.Token, filter: onlyOpensFileter);

            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }

            var model = ((IReadOnlyCollection<Position>)response).Select(r => 
                BitMexModelConverter.ExchangePositionToModel(r)).ToArray();
            return model;
        }

        private static IReadOnlyList<Order> EnsureCorrectResponse(string id, object response)
        {
            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }
            var res = (IReadOnlyList<Order>)response;
            if (res.Count != 1)
            {
                throw new InvalidOperationException($"Received {res.Count} orders. Expected exactly one with id {id}");
            }
            return res;
        }

        protected override void StartImpl()
        {
            OnConnected();
            _socketSubscriber.Start();
            _measureTimer = new Timer(OnMeasureTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
        }

        protected override void StopImpl()
        {
            _socketSubscriber.Stop();
            _measureTimer?.Dispose();
            _measureTimer = null;
        }

        private void OnMeasureTimer(object state)
        {
            _orderBooksHarvester.LogMeasures().Wait();
        }

        private async Task HandleOrder(TableResponse table)
        {
            if (!ValidateOrder(table))
            {
                await LykkeLog.WriteWarningAsync(nameof(BitMexExchange), nameof(HandleOrder),
                    $"Ignoring invalid 'order' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            switch (table.Action)
            {
                case Action.Insert:
                    var acks = table.Data.Select(row => _mapper.OrderToAck(row));
                    foreach (var ack in acks)
                    {
                        await CallAcknowledgementsHandlers(ack);
                    }
                    break;
                case Action.Update:
                    var trades = table.Data.Select(row => _mapper.OrderToTrade(row));
                    foreach (var trade in trades)
                    {
                        await CallExecutedTradeHandlers(trade);
                    }
                    break;
                case Action.Delete:
                default:
                    await LykkeLog.WriteWarningAsync(nameof(BitMexExchange), nameof(HandleOrder),
                        $"Ignoring 'order' message on table action {table.Action}. Message: '{JsonConvert.SerializeObject(table)}'");
                    break;
            }
        }

        private async Task HandleQuote(TableResponse table)
        {
            if (!ValidateQuote(table))
            {
                await LykkeLog.WriteWarningAsync(nameof(BitMexExchange), nameof(HandleQuote),
                    $"Ignoring invalid 'quote' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            if (table.Action == Action.Insert)
            {
                var prices = table.Data.Select(q => _mapper.QuoteToModel(q));
                foreach (var price in prices)
                {
                    await CallTickPricesHandlers(price);
                }
            }
            else
            {
                await LykkeLog.WriteWarningAsync(nameof(BitMexExchange), nameof(HandleQuote), 
                    $"Ignoring 'quote' message on table action={table.Action}. Message: '{JsonConvert.SerializeObject(table)}'");
            }
        }

        private async Task HandleOrderbook(TableResponse table)
        {
            var orderBookItems = table.Data.Select(o => o.ToOrderBookItem()).ToList();
            var groupByPair = orderBookItems.GroupBy(ob => ob.Symbol);

            switch (table.Action)
            {
                case Action.Partial:
                    foreach (var symbolGroup in groupByPair)
                    {
                        await _orderBooksHarvester.HandleOrdebookSnapshotAsync(symbolGroup.Key, DateTime.UtcNow, orderBookItems);
                    }
                    break;
                case Action.Update:
                case Action.Insert:
                case Action.Delete:
                    foreach (var symbolGroup in groupByPair)
                    {
                        await _orderBooksHarvester.HandleOrdersEventsAsync(symbolGroup.Key, ActionToOrderBookEventType(table.Action), orderBookItems);
                    }
                    break;
                default:
                    await LykkeLog.WriteWarningAsync(nameof(HandleOrderbook), "Parsing order book table response", $"Unknown table action {table.Action}");
                    break;
            }
        }

        private OrderBookEventType ActionToOrderBookEventType(Action action)
        {
            switch (action)
            {
                case Action.Update:
                    return OrderBookEventType.Update;
                case Action.Insert:
                    return OrderBookEventType.Add;
                case Action.Delete:
                    return OrderBookEventType.Delete;
                case Action.Unknown:
                case Action.Partial:
                    throw new NotSupportedException($"Order action {action} cannot be converted to OrderBookEventType");
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private bool ValidateQuote(TableResponse table)
        {
            return table != null
                && table.Data != null
                && table.Data.All(item => item.AskPrice.HasValue && item.BidPrice.HasValue);
        }

        private bool ValidateOrder(TableResponse table)
        {
            return table != null
                && table.Data != null
                && table.Data.All(item => 
                !string.IsNullOrEmpty(item.Symbol)
                && !string.IsNullOrEmpty(item.OrderID));
        }
    }
}
