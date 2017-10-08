using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Models;
using TradingBot.Models.Api;

namespace TradingBot.Controllers.Api
{
    public class ExchangesController : BaseApiController
    {
        public ExchangesController(ExchangeConnectorApplication app)
            : base(app)
        {
        }

        /// <summary>
        /// Get a list of all connected exchanges
        /// </summary>
        /// <remarks>The names of available exchanges participates in API calls for exchange-specific methods</remarks>
        /// <response code="200">An array of strings which are the names of exchanges</response>
        [HttpGet]
        public IEnumerable<string> List()
        {
            return Application.GetExchanges().Select(x => x.Name);
        }
        
        /// <summary>
        /// Get information about a specific exchange
        /// </summary>
        /// <param name="exchangeName">Name of the specific exchange</param>
        /// <response code="200">An information about the exchange, such as available trading instruments</response>
        /// <response code="400">Bad request response is returned in case of specifying name of unavailable exchange</response>
        [HttpGet("{exchangeName}")]
        [ProducesResponseType(typeof(ExchangeInformationModel), 200)]
        public IActionResult Index(string exchangeName)
        {
            var exchange = Application.GetExchange(exchangeName);
            
            if (exchange == null)
            {
                return BadRequest(new ResponseMessage($"There isn't connected exchange with the name of {exchangeName}. Try GET api/exchanges for the list of all connected exchanges."));
            }

            return Ok(new ExchangeInformationModel
                {
                    Name = exchangeName,
                    State = exchange.State,
                    Instruments = exchange.Instruments
                });
        }
    }
}
