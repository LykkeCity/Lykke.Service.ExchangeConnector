using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Models;
using TradingBot.Models.Api;

namespace TradingBot.Controllers.Api
{
    public class ExchangesController : BaseApiController
    {
        /// <summary>
        /// Get a list of all connected exchanges
        /// </summary>
        /// <remarks>The names of available exchanges participates in API calls for exchange-specific methods</remarks>
        /// <response code="200">An array of strings wich are the names of connected exchanges</response>
        [HttpGet]
        public IReadOnlyCollection<string> List()
        {
            return Application.GetConnectedExchanges();
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
            if (!Application.GetConnectedExchanges().Contains(exchangeName))
            {
                return BadRequest(new ResponseMessage($"There isn't connected exchange with the name of {exchangeName}. Try GET api/exchanges for the list of all connected exchanges."));
            }

            return Ok(new ExchangeInformationModel
                {
                    Name = exchangeName,
                    Instruments = Application.GetExchange(exchangeName).Instruments
                });
        }
    }
}