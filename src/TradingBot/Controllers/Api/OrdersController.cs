using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Exchanges.Concrete.Icm;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models;
using TradingBot.Models.Api;
using TradingBot.Trading;

namespace TradingBot.Controllers.Api
{
    public class OrdersController : BaseApiController
    {
        public ResponseMessage Index()
        {
            return new ResponseMessage("You have to specify exchange name to get the actual orders");
        }
        
        [HttpGet("{exchangeName}")]
        public async Task<object> Index(string exchangeName)
        {
            try
            {
                var exchange = Application.GetExchange(exchangeName);

                if (exchange is IcmExchange)
                {
                    return await ((IcmExchange) exchange).GetAllOrdersInfo();
                }

                return Application.GetExchange(exchangeName).ActualOrders;
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.BadRequest, e.Message);
            }
        }

        [HttpGet("{exchangeName}/{instrument}/{id}")]
        public async Task<ExecutedTrade> GetOrder(string exchangeName, string instrument, long id)
        {
            var exchange = Application.GetExchange(exchangeName);

            if (exchange is IcmExchange)
                return await ((IcmExchange) exchange).GetOrderInfo(new Instrument(exchangeName, instrument), id);
            
            //var order = Application.GetExchange(exchangeName).ActualOrders[instrument].SingleOrDefault(x => x.OrderId == id);

            return null;
        }

        [ApiKeyAuth]
        [HttpPost("{exchangeName}")]
        public async Task<IActionResult> Post(string exchangeName, [FromBody] OrderModel orderModel)
        {   
            if (orderModel == null) 
                throw new StatusCodeException(HttpStatusCode.BadRequest, "Order have to be specified");
            
            // TODO: move validation logic into the model
            if (Math.Abs((orderModel.DateTime - DateTime.UtcNow).TotalMilliseconds) >= TimeSpan.FromMinutes(5).TotalMilliseconds)
                ModelState.AddModelError(nameof(orderModel.DateTime), "Date and time must be in 5 minutes threshold from UTC now");

            if (orderModel.Price == 0 && orderModel.OrderType != OrderType.Market)
                ModelState.AddModelError(nameof(orderModel.Price), "Price have to be declared for non-market orders");
            
            if (!ModelState.IsValid)
                throw new StatusCodeException(ModelState);
            
            try
            {

                var instrument = new Instrument(exchangeName, orderModel.Instrument);
                var tradingSignal = new TradingSignal(orderModel.Id, OrderCommand.Create, orderModel.TradeType, orderModel.Price, orderModel.Volume, DateTime.UtcNow, orderModel.OrderType);
            
                if (exchangeName == IcmExchange.Name)
                {
                    var result = await ((IcmExchange) Application.GetExchange(exchangeName))
                        .AddOrderAndWait(instrument, tradingSignal, Configuration.Instance.AspNet.ApiTimeout);

                    if (result.Status == ExecutionStatus.Rejected || result.Status == ExecutionStatus.Cancelled)
                        return BadRequest(new ResponseMessage($"Exchange return status: {result.Status}"));
                
                    return CreatedAtAction("GetOrder", 
                        new { exchangeName = exchangeName, instrument = orderModel.Instrument, id = orderModel.Id}, 
                        result);
                }
                else
                {
                    await Application.GetExchange(exchangeName)
                        .PlaceTradingOrders(new InstrumentTradingSignals(instrument, new [] { tradingSignal }));
                
                    return StatusCode((int)HttpStatusCode.Created);
                }
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message);
            }
        }


        [ApiKeyAuth]
        [HttpDelete("{exchangeName}")]
        public async Task<IActionResult> CancelOrder(string exchangeName, [FromBody] OrderModel orderModel)
        {
            if (orderModel == null) 
                throw new StatusCodeException(HttpStatusCode.BadRequest, "Order have to be specified");
            
            // TODO: move validation logic into the model
            if (Math.Abs((orderModel.DateTime - DateTime.UtcNow).TotalMilliseconds) >= TimeSpan.FromMinutes(5).TotalMilliseconds)
                ModelState.AddModelError(nameof(orderModel.DateTime), "Date and time must be in 5 minutes threshold from UTC now");

            if (orderModel.Price == 0 && orderModel.OrderType != OrderType.Market)
                ModelState.AddModelError(nameof(orderModel.Price), "Price have to be declared for non-market orders");
            
            if (!ModelState.IsValid)
                throw new StatusCodeException(ModelState);
            
            try
            {

                var instrument = new Instrument(exchangeName, orderModel.Instrument);
                var tradingSignal = new TradingSignal(orderModel.Id, OrderCommand.Cancel, orderModel.TradeType, orderModel.Price, orderModel.Volume, DateTime.UtcNow, orderModel.OrderType);
            
                if (exchangeName == IcmExchange.Name)
                {
                    var result = await ((IcmExchange) Application.GetExchange(exchangeName))
                        .CancelOrderAndWait(instrument, tradingSignal, Configuration.Instance.AspNet.ApiTimeout);

                    if (result.Status == ExecutionStatus.Rejected)
                        return BadRequest(new ResponseMessage($"Exchange return status: {result.Status}"));
                
                    return Accepted(result);
                }
                else
                {
                    await Application.GetExchange(exchangeName)
                        .PlaceTradingOrders(new InstrumentTradingSignals(instrument, new [] { tradingSignal }));
                
                    return Accepted();
                }
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}