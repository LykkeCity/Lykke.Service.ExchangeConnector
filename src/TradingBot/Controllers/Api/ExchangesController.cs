using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Infrastructure.Monitoring;
using TradingBot.Models;
using TradingBot.Models.Api;

namespace TradingBot.Controllers.Api
{
    public sealed class ExchangesController : BaseApiController
    {
        private readonly IExchangeRatingValuer _ratingValuer;

        public ExchangesController(IApplicationFacade app, IExchangeRatingValuer ratingValuer)
            : base(app)
        {
            _ratingValuer = ratingValuer;
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


        /// <summary>
        /// Returns ratings of exchanges
        /// </summary>
        /// <returns>A collection of ratings for each enabled exchange</returns>
        [HttpGet("rating")]
        [ProducesResponseType(typeof(IEnumerable<ExchangeRatingModel>), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        public IActionResult GetRating()
        {
            return Ok(_ratingValuer.Rating);
        }
    }
}
