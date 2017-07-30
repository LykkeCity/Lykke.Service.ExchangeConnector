using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Models;

namespace TradingBot.Controllers.Api
{
    public class ExchangesController : BaseApiController
    {
        [HttpGet]
        public IReadOnlyCollection<string> List()
        {
            return Application.GetConnectedExchanges();
        }
        
        [HttpGet("{exchangeName}")]
        public IActionResult Index(string exchangeName)
        {
            if (!Application.GetConnectedExchanges().Contains(exchangeName))
            {
                return BadRequest(new ResponseMessage($"There isn't connected exchange with the name of {exchangeName}. Try GET api/exchanges for the list of all connected exchanges."));
            }

            return Ok(Application.GetExchange(exchangeName).Instruments);
        }
    }
}