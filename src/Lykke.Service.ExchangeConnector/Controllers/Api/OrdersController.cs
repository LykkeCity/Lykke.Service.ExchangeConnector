using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using TradingBot.Communications;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Controllers.Api
{
    [ApiKeyAuth]
    public sealed class OrdersController : BaseApiController
    {
        private readonly TimeSpan _timeout;

        private readonly TranslatedSignalsRepository _translatedSignalsRepository;

        public OrdersController(IApplicationFacade app, AppSettings appSettings, TranslatedSignalsRepository translatedSignalsRepository)
            : base(app)
        {
            _translatedSignalsRepository = translatedSignalsRepository;
            _timeout = appSettings.AspNet.ApiTimeout;
        }

        /// <summary>
        /// Returns information about all OPEN orders on the exchange
        /// <param name="exchangeName">The name of the exchange</param>
        /// </summary>
        [SwaggerOperation("GetOpenedOrders")]
        [HttpGet]
        private async Task<IEnumerable<ExecutionReport>> Index([FromQuery, Required] string exchangeName) // Intentionally disabled
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);
                return await exchange.GetOpenOrders(_timeout);
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.BadRequest, e.Message, e);
            }
        }

        /// <summary>
        /// Returns information about the earlier placed order
        /// </summary>
        /// <param name="id">The order id</param>
        /// <param name="instrument">The instrument name of the order</param>
        /// <param name="exchangeName">The exchange name</param>
        [SwaggerOperation("GetOrder")]
        [HttpGet("{id}")]
        public async Task<ExecutionReport> GetOrder(string id, [FromQuery, Required] string exchangeName, [FromQuery, Required] string instrument)
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);
                return await exchange.GetOrder(id, new Instrument(exchangeName, instrument), _timeout);

            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }


        /// <summary>
        /// Places a new order on the exchange
        /// </summary>
        /// <param name="orderModel">A new order</param>
        /// <remarks>In the location header of successful response placed an URL for getting info about the order</remarks>
        [SwaggerOperation("CreateOrder")]
        [HttpPost]
        public async Task<ExecutionReport> Post([FromBody] OrderModel orderModel)
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
                    throw new StatusCodeException(HttpStatusCode.BadRequest) { Model = new SerializableError(ModelState) };


                var instrument = new Instrument(orderModel.ExchangeName, orderModel.Instrument);
                var tradingSignal = new TradingSignal(instrument, GetUniqueOrderId(orderModel), OrderCommand.Create, orderModel.TradeType,
                    orderModel.Price, orderModel.Volume, DateTime.UtcNow,
                    orderModel.OrderType, orderModel.TimeInForce);

                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RestApi, tradingSignal)
                {
                    ClientIP = HttpContext.Connection.RemoteIpAddress.ToString()
                };

                try
                {
                    var result = await Application.GetExchange(orderModel.ExchangeName)
                        .AddOrderAndWaitExecution(tradingSignal, translatedSignal, _timeout);

                    translatedSignal.SetExecutionResult(result);

                    return result;
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
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }

        private static string GetUniqueOrderId(OrderModel orderModel)
        {
            return orderModel.ExchangeName + DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Cancels the existing order
        /// </summary>
        /// <remarks></remarks>
        /// <param name="id">The order id to cancel</param>
        /// <param name="exchangeName">The exchange name</param>
        [SwaggerOperation("CancelOrder")]
        [HttpDelete("{id}")]
        public async Task<ExecutionReport> CancelOrder(string id, [FromQuery, Required]string exchangeName)
        {
            try
            {
                if (string.IsNullOrEmpty(exchangeName))
                {
                    throw new StatusCodeException(HttpStatusCode.InternalServerError, "Exchange has to be specified");
                }

                var instrument = new Instrument(exchangeName, string.Empty);
                var tradingSignal = new TradingSignal(instrument, id, OrderCommand.Cancel, TradeType.Unknown, 0, 0, DateTime.UtcNow, OrderType.Unknown);

                var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RestApi, tradingSignal)
                {
                    ClientIP = HttpContext.Connection.RemoteIpAddress.ToString()
                };

                try
                {
                    var result = await Application.GetExchange(exchangeName)
                        .CancelOrderAndWaitExecution(tradingSignal, translatedSignal, _timeout);

                    if (result.ExecutionStatus == OrderExecutionStatus.Rejected)
                        throw new StatusCodeException(HttpStatusCode.BadRequest, $"Exchange return status: {result.ExecutionStatus}", null);

                    return result;
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
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }
    }
}
