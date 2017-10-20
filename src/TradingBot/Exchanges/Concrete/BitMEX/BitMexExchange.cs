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
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;
using Position = TradingBot.Exchanges.Concrete.AutorestClient.Models.Position;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexExchange : Exchange
    {
        private readonly BitMexExchangeConfiguration _configuration;
        private readonly IBitMEXAPI _exchangeApi;
        public const string BitMex = "bitmex";

        public BitMexExchange(BitMexExchangeConfiguration configuration, TranslatedSignalsRepository translatedSignalsRepository, ILog log) : base(BitMex, configuration, translatedSignalsRepository, log)
        {
            _configuration = configuration;
            var credenitals = new BitMexServiceClientCredentials(_configuration.ApiKey, _configuration.ApiSecret);
            _exchangeApi = new BitMEXAPI(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl)
            };
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = BitMexModelConverter.ConvertSymbolFromLykkeToBitMex(instrument.Name, _configuration);
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

            return new ExecutedTrade(instrument, exceTime, execPrice, execVolume, execType, order.OrderID, execStatus) { Message = order.Text };
        }



        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var ct = new CancellationTokenSource(timeout);
            var id = signal.OrderId;
            var response = await _exchangeApi.OrdercancelAsync(cancellationToken: ct.Token, orderID: id);

            if (response is Error error)
            {
                throw new ApiException(error.ErrorProperty.Message);
            }
            var res = EnsureCorrectResponse(id, response);
            return BitMexModelConverter.OrderToTrade(res[0], _configuration);
        }

        public override async Task<ExecutedTrade> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            var filterObj = new { orderID = id };
            var filterArg = JsonConvert.SerializeObject(filterObj);
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.OrdergetOrdersAsync(filter: filterArg, cancellationToken: cts.Token);
            var res = EnsureCorrectResponse(id, response);
            return BitMexModelConverter.OrderToTrade(res[0], _configuration);
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

            var trades = ((IReadOnlyCollection<Order>)response).Select(r => BitMexModelConverter.OrderToTrade(r, _configuration));
            return trades;
        }

        public override async Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.UsergetMarginWithHttpMessagesAsync(cancellationToken: cts.Token);
            var bitmexMargin = response.Body;

            var model = BitMexModelConverter.ExchangeBalanceToModel(bitmexMargin, _configuration);
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

            var model = ((IReadOnlyCollection<Position>)response).Select(r => BitMexModelConverter.ExchangePositionToModel(r, _configuration)).ToArray();
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
        }

        protected override void StopImpl()
        {

        }
    }
}
