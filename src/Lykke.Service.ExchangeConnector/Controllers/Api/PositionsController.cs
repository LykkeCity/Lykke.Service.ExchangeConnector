using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models;
using TradingBot.Models.Api;

namespace TradingBot.Controllers.Api
{
    [ApiKeyAuth]
    public sealed class PositionsController : BaseApiController
    {
        private readonly TimeSpan _timeout;

        public PositionsController(IApplicationFacade app, AppSettings appSettings)
            : base(app)
        {
            _timeout = appSettings.AspNet.ApiTimeout;

        }

        /// <summary>
        /// Returns information about opened positions
        /// </summary>
        /// <param name="exchangeName">The exchange name</param>
        /// <response code="200">Active positions</response>
        /// <response code="500">Unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PositionModel>), 200)]
        [ProducesResponseType(typeof(ResponseMessage), 500)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Index([FromQuery, Required] string exchangeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(exchangeName))
                {
                    return BadRequest($"Invalid {nameof(exchangeName)}");
                }
                var exchange = Application.GetExchange(exchangeName);
                return Ok(await exchange.GetPositions(_timeout));

            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }
    }
}
