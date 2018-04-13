using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using TradingBot.Models.Api;

namespace TradingBot.Controllers.Api
{
    public sealed class ExchangesController : BaseApiController
    {
        public ExchangesController(IApplicationFacade app)
            : base(app)
        {
        }

        /// <summary>
        /// Get a list of all connected exchanges
        /// </summary>
        /// <remarks>The names of available exchanges participates in API calls for exchange-specific methods</remarks>
        [HttpGet]
        [SwaggerOperation("GetSupportedExchanges")]
        public IEnumerable<string> List()
        {
            return Application.GetExchanges().Select(x => x.Name);
        }

        /// <summary>
        /// Get information about a specific exchange
        /// </summary>
        /// <param name="exchangeName">Name of the specific exchange</param>
        [SwaggerOperation("GetExchangeInfo")]
        [HttpGet("{exchangeName}")]
        [ProducesResponseType(typeof(ExchangeInformationModel), 200)]
        public IActionResult Index(string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName) || Application.GetExchange(exchangeName) == null)
            {
                return BadRequest($"Invalid {nameof(exchangeName)}");
            }
            var exchange = Application.GetExchange(exchangeName);

            return Ok(new ExchangeInformationModel
            {
                Name = exchangeName,
                State = exchange.State,
                Instruments = exchange.Instruments,
                StreamingSupport = exchange.StreamingSupport
            });
        }
    }
}
