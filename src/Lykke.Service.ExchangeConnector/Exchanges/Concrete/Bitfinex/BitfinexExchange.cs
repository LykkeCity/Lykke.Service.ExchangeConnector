using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exceptions;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;
using Instrument = TradingBot.Trading.Instrument;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model;
using Order = Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model.Order;
using Position = Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model.Position;
using Error = Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model.Error;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal class BitfinexExchange : Exchange
    {
        private readonly BitfinexModelConverter _modelConverter;
        private readonly BitfinexOrderBooksHarvester _orderBooksHarvester;
        private readonly BitfinexExecutionHarvester _executionHarvester;
        private readonly IBitfinexApi _exchangeApi;
        public new const string Name = "bitfinex";

        public BitfinexExchange(BitfinexExchangeConfiguration configuration,
            TranslatedSignalsRepository translatedSignalsRepository,
            BitfinexOrderBooksHarvester orderBooksHarvester,
            BitfinexExecutionHarvester executionHarvester, ILog log)
            : base(Name, configuration, translatedSignalsRepository, log)
        {
            _modelConverter = new BitfinexModelConverter(configuration);
            _orderBooksHarvester = orderBooksHarvester;
            _executionHarvester = executionHarvester;
            var credenitals = new BitfinexServiceClientCredentials(configuration.ApiKey, configuration.ApiSecret);
            _exchangeApi = new BitfinexApi(credenitals)
            {
                BaseUri = new Uri(configuration.EndpointUrl)
            };



            orderBooksHarvester.MaxOrderBookRate = configuration.MaxOrderBookRate;
        }

        public override async Task<ExecutionReport> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var symbol = _modelConverter.LykkeSymbolToExchangeSymbol(signal.Instrument.Name);
            var volume = signal.Volume;
            var orderType = _modelConverter.ConvertOrderType(signal.OrderType);
            var side = _modelConverter.ConvertTradeType(signal.TradeType);
            var price = signal.Price == 0 ? 1 : signal.Price ?? 1;
            var cts = new CancellationTokenSource(timeout);

            var response = await _exchangeApi.AddOrder(symbol, volume, price, side, orderType, cts.Token);

            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }

            var trade = OrderToTrade((Order)response);
            return trade;
        }

        public override async Task<ExecutionReport> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {

            var cts = new CancellationTokenSource(timeout);
            if (!long.TryParse(signal.OrderId, out var id))
            {
                throw new ApiException("Bitfinex order id can be only integer");
            }
            var response = await _exchangeApi.CancelOrder(id, cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var trade = OrderToTrade((Order)response);
            return trade;
        }

        public override async Task<ExecutionReport> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            if (!long.TryParse(id, out var orderId))
            {
                throw new ApiException("Bitfinex order id can be only integer");
            }
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.GetOrderStatus(orderId, cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var trade = OrderToTrade((Order)response);
            return trade;
        }

        public override async Task<IEnumerable<ExecutionReport>> GetOpenOrders(TimeSpan timeout)
        {

            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.GetActiveOrders(cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var trades = ((IReadOnlyCollection<Order>)response).Select(OrderToTrade);
            return trades;
        }

        public override async Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.GetActivePositions(cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var marginInfo = await GetMarginInfo(timeout);
            var positions = ExchangePositionsToPositionModel((IReadOnlyCollection<Position>)response, marginInfo);
            return positions;
        }

        private IReadOnlyCollection<PositionModel> ExchangePositionsToPositionModel(IEnumerable<Position> response, IReadOnlyList<MarginInfo> marginInfo)
        {
            var marginByCurrency = marginInfo[0].MarginLimits.ToDictionary(ml => ml.OnPair, ml => ml, StringComparer.InvariantCultureIgnoreCase);
            var result = response.Select(r =>
                new PositionModel
                {
                    Symbol = _modelConverter.ExchangeSymbolToLykkeInstrument(r.Symbol).Name,
                    PositionVolume = r.Amount,
                    MaintMarginUsed = r.Amount * r.Base * marginByCurrency[r.Symbol].MarginRequirement / 100m,
                    RealisedPnL = 0, //TODO no specification,
                    UnrealisedPnL = r.Pl,
                    PositionValue = r.Amount * r.Base,
                    AvailableMargin = Math.Max(0, marginByCurrency[r.Symbol].TradableBalance) * marginByCurrency[r.Symbol].InitialMargin / 100m,
                    InitialMarginRequirement = marginByCurrency[r.Symbol].InitialMargin / 100m,
                    MaintenanceMarginRequirement = marginByCurrency[r.Symbol].MarginRequirement / 100m
                }
            );
            return result.ToArray();
        }

        public override async Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout)
        {
            var marginInfor = await GetMarginInfo(timeout);
            var result = MarginInfoToBalance(marginInfor);
            return result;
        }

        private async Task<IReadOnlyList<MarginInfo>> GetMarginInfo(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            var response = await _exchangeApi.GetMarginInformation(cts.Token);
            if (response is Error error)
            {
                throw new ApiException(error.Message);
            }
            var marginInfor = (IReadOnlyList<MarginInfo>)response;

            return marginInfor;
        }

        private static IReadOnlyCollection<TradeBalanceModel> MarginInfoToBalance(IReadOnlyList<MarginInfo> marginInfos)
        {
            if (marginInfos.Count != 1)
            {
                throw new ApiException(@"Incorrect number of marginInfo. Expected 1 but received {marginInfo.Count}");
            }

            var mi = marginInfos[0];
            var balance = new TradeBalanceModel
            {
                AccountCurrency = "USD",
                Totalbalance = mi.NetValue,
                UnrealisedPnL = mi.UnrealizedPl,
                MaringAvailable = 0, // TODO The mapping is not defined yet.
                MarginUsed = mi.RequiredMargin
            };

            return new[] { balance };
        }

        private ExecutionReport OrderToTrade(Order order)
        {
            var id = order.Id;
            var execTime = order.Timestamp;
            var execPrice = order.Price;
            var execVolume = order.ExecutedAmount;
            var tradeType = BitfinexModelConverter.ConvertTradeType(order.Side);
            var status = ConvertExecutionStatus(order);
            var instr = _modelConverter.ExchangeSymbolToLykkeInstrument(order.Symbol);

            return new ExecutionReport(instr, execTime, execPrice, execVolume, tradeType, id, status)
            {
                ExecType = ExecType.Trade,
                Success = true,
                FailureType = OrderStatusUpdateFailureType.None
            };
        }

        protected override void StartImpl()
        {
            _executionHarvester.Start();
            _orderBooksHarvester.Start();
            OnConnected();
        }

        protected override void StopImpl()
        {
            _executionHarvester.Stop();
            _orderBooksHarvester.Stop();
        }



        private static OrderExecutionStatus ConvertExecutionStatus(Order order)
        {
            if (order.IsCancelled)
            {
                return OrderExecutionStatus.Cancelled;
            }
            if (order.IsLive)
            {
                return OrderExecutionStatus.New;
            }
            return OrderExecutionStatus.Fill;
        }

    }
}
