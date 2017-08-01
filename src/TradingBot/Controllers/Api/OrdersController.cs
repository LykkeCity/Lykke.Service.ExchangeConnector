using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Concrete.ICMarkets;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models;
using TradingBot.Models.Api;

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
            var exchange = Application.GetExchange(exchangeName);
            
            if (exchange is ICMarketsExchange)
            {
                return await ((ICMarketsExchange) exchange).GetOrdersCountAsync();
            }
            
            return Application.GetExchange(exchangeName).ActualOrders;
        }

        [HttpGet("{exchangeName}/{instrument}/{id}")]
        public TradingSignal GetOrder(string exchangeName, string instrument, long id)
        {
            var order = Application.GetExchange(exchangeName).ActualOrders[instrument].SingleOrDefault(x => x.OrderId == id);

            return order;
        }

        [HttpPost("{exchangeName}")]
        public async Task<IActionResult> Post(string exchangeName, [FromBody] OrderModel orderModel)
        {
            // TODO: check DateTime interval (have to be in 5 minutes threshold)
            
            if (orderModel == null) 
                return BadRequest(new ResponseMessage("Order have to be specified"));

            if(orderModel.Price == 0 && orderModel.OrderType != OrderType.Market)
                ModelState.AddModelError(nameof(orderModel.Price), "Price have to be declared for non-market orders");
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var instrument = new Instrument(exchangeName, orderModel.Instrument);
            var tradingSignal = new TradingSignal(orderModel.Id, OrderCommand.Create, orderModel.TradeType, orderModel.Price, orderModel.Volume, DateTime.UtcNow, orderModel.OrderType);
            
            if (exchangeName == ICMarketsExchange.Name)
            {
                var result = await ((ICMarketsExchange) Application.GetExchange(exchangeName))
                    .AddOrderAndWait(instrument, tradingSignal, Configuration.Instance.AspNet.ApiTimeout);

                int statusCode = (int) HttpStatusCode.Created;
                if (result.Status == ExecutionStatus.Rejected || result.Status == ExecutionStatus.Cancelled)
                    statusCode = (int) HttpStatusCode.Forbidden;
                
                return StatusCode(statusCode, result);
            }
            else
            {
                
                await Application.GetExchange(exchangeName)
                    .PlaceTradingOrders(new InstrumentTradingSignals(instrument, new [] { tradingSignal }));
                
            
                return StatusCode((int)HttpStatusCode.Created);
            }
        }
    }
}