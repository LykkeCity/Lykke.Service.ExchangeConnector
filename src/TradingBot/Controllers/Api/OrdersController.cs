using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Icm;
using TradingBot.Exchanges.Concrete.Kraken;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models;
using TradingBot.Models.Api;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Controllers.Api
{
    public class OrdersController : BaseApiController
    {
        private readonly TimeSpan _timeout;

        private readonly TranslatedSignalsRepository translatedSignalsRepository;

        public OrdersController(ExchangeConnectorApplication app, AppSettings appSettings)
            : base(app)
        {
            translatedSignalsRepository = Application.TranslatedSignalsRepository;
            _timeout = appSettings.AspNet.ApiTimeout;
        }

        /// <summary>
        /// Get information about all current orders on exchange
        /// </summary>
        [HttpGet("{exchangeName}")]
        public async Task<object> Index(string exchangeName)
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);

                if (exchange is IcmExchange)
                {
                    return await ((IcmExchange)exchange).GetAllOrdersInfo(_timeout);
                }
                else if (exchange is KrakenExchange)
                {
                    return await ((KrakenExchange)exchange).GetOpenOrders(CancellationToken.None);
                }

                return Application.GetExchange(exchangeName).ActualOrders;
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.BadRequest, e.Message);
            }
        }

        /// <summary>
        /// Get information about earlier placed order
        /// </summary>
        [HttpGet("{exchangeName}/{instrument}/{id}")]
        public async Task<ExecutedTrade> GetOrder(string exchangeName, string instrument, string id)
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);

                if (exchange is IcmExchange)
                    return await ((IcmExchange)exchange).GetOrderInfo(new Instrument(exchangeName, instrument), id);
                else
                    throw new NotSupportedException("Get orders method is supported for ICM only");
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message);
            }
        }


        /// <summary>
        /// Place a new order to the exchange
        /// </summary>
        /// <remarks>In the location header of succesful response placed an URL for getting info about the order</remarks>
        /// <response code="200">The order is successfully placed and order status is returned</response>
        /// <response code="400">Can't place the order. The reason is in the response</response>
        [ApiKeyAuth]
        [HttpPost("{exchangeName}")]
        [ProducesResponseType(typeof(ExecutedTrade), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 400)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        public async Task<IActionResult> Post(string exchangeName, [FromBody] OrderModel orderModel)
        {
            try
            {
                if (orderModel == null)
                    throw new StatusCodeException(HttpStatusCode.BadRequest, "Order have to be specified");

                if (Math.Abs((orderModel.DateTime - DateTime.UtcNow).TotalMilliseconds) >=
                    TimeSpan.FromMinutes(5).TotalMilliseconds)
                    ModelState.AddModelError(nameof(orderModel.DateTime),
                        "Date and time must be in 5 minutes threshold from UTC now");

                if (orderModel.Price == 0 && orderModel.OrderType != OrderType.Market)
                    ModelState.AddModelError(nameof(orderModel.Price),
                        "Price have to be declared for non-market orders");

                if (!ModelState.IsValid)
                    throw new StatusCodeException(ModelState);


                var instrument = new Instrument(exchangeName, orderModel.Instrument);
                var tradingSignal = new TradingSignal(orderModel.Id, OrderCommand.Create, orderModel.TradeType,
                    orderModel.Price, orderModel.Volume, DateTime.UtcNow,
                    orderModel.OrderType, orderModel.TimeInForce);

                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RestApi, instrument.Exchange,
                    instrument.Name, tradingSignal)
                {
                    ClientIP = HttpContext.Connection.RemoteIpAddress.ToString()
                };

                try
                {
                    var result = await Application.GetExchange(exchangeName)
                        .AddOrderAndWaitExecution(instrument, tradingSignal, translatedSignal, _timeout);

                    translatedSignal.SetExecutionResult(result);

                    if (result.Status == ExecutionStatus.Rejected || result.Status == ExecutionStatus.Cancelled)
                        throw new StatusCodeException(HttpStatusCode.BadRequest,
                            $"Exchange return status: {result.Status}");

                    return CreatedAtAction("GetOrder",
                        new { exchangeName = exchangeName, instrument = orderModel.Instrument, id = orderModel.Id },
                        result);
                }
                catch (Exception e)
                {
                    translatedSignal.Failure(e);
                    throw;
                }
                finally
                {
                    await translatedSignalsRepository.SaveAsync(translatedSignal);
                }
            }
            catch (StatusCodeException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message);
            }
        }


        /// <summary>
        /// Cancel existing order
        /// </summary>
        /// <remarks></remarks>
        /// <response code="200">The order is successfully canceled</response>
        /// <response code="400">Can't cancel the order. The reason is in the response</response>
        [ApiKeyAuth]
        [HttpDelete("{exchangeName}")]
        [ProducesResponseType(typeof(ExecutedTrade), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 400)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        public async Task<IActionResult> CancelOrder(string exchangeName, [FromBody] OrderModel orderModel)
        {
            try
            {
                if (orderModel == null)
                    throw new StatusCodeException(HttpStatusCode.BadRequest, "Order have to be specified");

                if (Math.Abs((orderModel.DateTime - DateTime.UtcNow).TotalMilliseconds) >=
                    TimeSpan.FromMinutes(5).TotalMilliseconds)
                    ModelState.AddModelError(nameof(orderModel.DateTime),
                        "Date and time must be in 5 minutes threshold from UTC now");

                if (orderModel.Price == 0 && orderModel.OrderType != OrderType.Market)
                    ModelState.AddModelError(nameof(orderModel.Price),
                        "Price have to be declared for non-market orders");

                if (!ModelState.IsValid)
                    throw new StatusCodeException(ModelState);

                var instrument = new Instrument(exchangeName, orderModel.Instrument);
                var tradingSignal = new TradingSignal(orderModel.Id, OrderCommand.Cancel, orderModel.TradeType,
                    orderModel.Price, orderModel.Volume, DateTime.UtcNow, orderModel.OrderType);

                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RestApi, exchangeName, orderModel.Instrument, tradingSignal)
                {
                    ClientIP = HttpContext.Connection.RemoteIpAddress.ToString()
                };

                try
                {
                    var result = await Application.GetExchange(exchangeName)
                        .CancelOrderAndWaitExecution(instrument, tradingSignal, translatedSignal, _timeout);

                    if (result.Status == ExecutionStatus.Rejected)
                        throw new StatusCodeException(HttpStatusCode.BadRequest, $"Exchange return status: {result.Status}");

                    return Ok(result);
                }
                catch (Exception e)
                {
                    translatedSignal.Failure(e);
                    throw;
                }
                finally
                {
                    await translatedSignalsRepository.SaveAsync(translatedSignal);
                }
            }
            catch (StatusCodeException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}
