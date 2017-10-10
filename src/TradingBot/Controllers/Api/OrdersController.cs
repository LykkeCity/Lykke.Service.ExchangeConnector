using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
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

        private readonly TranslatedSignalsRepository _translatedSignalsRepository;

        public OrdersController(ExchangeConnectorApplication app, AppSettings appSettings)
            : base(app)
        {
            _translatedSignalsRepository = Application.TranslatedSignalsRepository;
            _timeout = appSettings.AspNet.ApiTimeout;
        }

        /// <summary>
        /// Get information about all OPEN orders on the exchange
        /// <param name="exchangeName">The name of the exchange</param>
        /// </summary>
        [HttpGet]
        public async Task<object> Index([FromQuery, Required] string exchangeName)
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);

                switch (exchange)
                {
                    case IcmExchange e:
                        return await e.GetOpenOrders(_timeout);
                    case KrakenExchange e:
                        return await e.GetOpenOrders(_timeout);
                    default:
                        throw new NotSupportedException();
                }
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.BadRequest, e.Message);
            }
        }

        /// <summary>
        /// Get information about earlier placed order
        /// </summary>
        /// <param name="id">The order id</param>
        /// <param name="instrument">The instrument name of the order</param>
        /// <param name="exchangeName">The exchange name</param>
        /// <response code="200">The order is found</response>
        /// <response code="500">The order either not exist or other server error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExecutedTrade), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        public async Task<ExecutedTrade> GetOrder(string id, [FromQuery, Required] string exchangeName, [FromQuery, Required] string instrument)
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);
                return await exchange.GetOrder(id, new Instrument(exchangeName, instrument));

            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message);
            }
        }


        /// <summary>
        /// Place a new order to the exchange
        ///<param name="orderModel">A new order</param>
        /// </summary>
        /// <remarks>In the location header of successful response placed an URL for getting info about the order</remarks>
        /// <response code="200">The order is successfully placed and order status is returned</response>
        /// <response code="400">Can't place the order. The reason is in the response</response>
        [ApiKeyAuth]
        [HttpPost]
        [ProducesResponseType(typeof(ExecutedTrade), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 400)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        public async Task<IActionResult> Post([FromBody] OrderModel orderModel)
        {
            try
            {
                if (orderModel == null)
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest, "Order has to be specified");
                }
                if (string.IsNullOrEmpty(orderModel.ExchangeName))
                {
                    ModelState.AddModelError(nameof(orderModel.ExchangeName), "Exchange cannot be null");
                }

                if (Math.Abs((orderModel.DateTime - DateTime.UtcNow).TotalMilliseconds) >=
                    TimeSpan.FromMinutes(5).TotalMilliseconds)
                    ModelState.AddModelError(nameof(orderModel.DateTime),
                        "Date and time must be in 5 minutes threshold from UTC now");

                if (orderModel.Price == 0 && orderModel.OrderType != OrderType.Market)
                    ModelState.AddModelError(nameof(orderModel.Price),
                        "Price have to be declared for non-market orders");

                if (!ModelState.IsValid)
                    throw new StatusCodeException(ModelState);


                var instrument = new Instrument(orderModel.ExchangeName, orderModel.Instrument);
                var tradingSignal = new TradingSignal(null, OrderCommand.Create, orderModel.TradeType,
                    orderModel.Price, orderModel.Volume, DateTime.UtcNow,
                    orderModel.OrderType, orderModel.TimeInForce);

                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RestApi, instrument.Exchange,
                    instrument.Name, tradingSignal)
                {
                    ClientIP = HttpContext.Connection.RemoteIpAddress.ToString()
                };

                try
                {
                    var result = await Application.GetExchange(orderModel.ExchangeName)
                        .AddOrderAndWaitExecution(instrument, tradingSignal, translatedSignal, _timeout);

                    translatedSignal.SetExecutionResult(result);

                    if (result.Status == ExecutionStatus.Rejected || result.Status == ExecutionStatus.Cancelled)
                        throw new StatusCodeException(HttpStatusCode.BadRequest,
                            $"Exchange return status: {result.Status}");

                    return Ok(result);
                }
                catch (Exception e)
                {
                    translatedSignal.Failure(e);
                    throw;
                }
                finally
                {
                    await _translatedSignalsRepository.SaveAsync(translatedSignal);
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
        /// <param name="id">The order id to cancel</param>
        /// <param name="exchangeName">The exchange name</param>
        /// <response code="200">The order is successfully canceled</response>
        /// <response code="400">Can't cancel the order. The reason is in the response</response>
        [ApiKeyAuth]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ExecutedTrade), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 400)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        public async Task<IActionResult> CancelOrder(string id, [FromQuery, Required]string exchangeName)
        {
            try
            {
                if (string.IsNullOrEmpty(exchangeName))
                {
                    throw new StatusCodeException(HttpStatusCode.BadRequest, "Exchange has to be specified");
                }

                var instrument = new Instrument(exchangeName, string.Empty);
                var tradingSignal = new TradingSignal(id, OrderCommand.Cancel, TradeType.Unknown, 0, 0, DateTime.UtcNow, OrderType.Unknown);

                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RestApi, exchangeName, instrument.Name, tradingSignal)
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
                    await _translatedSignalsRepository.SaveAsync(translatedSignal);
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
