using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lykke.ExternalExchangesApi.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;

namespace TradingBot.Controllers.Api
{
    //[ApiKeyAuth]
    [Authorize]
    [SignatureHeaders]
    //[Route("api/v1/[controller]")]
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
        [SwaggerOperation("GetOpenedPosition")]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<PositionModel>), 200)]
        public async Task<IActionResult> Index([FromQuery, Required] string exchangeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(exchangeName) || Application.GetExchange(exchangeName) == null)
                {
                    return BadRequest($"Invalid {nameof(exchangeName)}");
                }
                var exchange = Application.GetExchange(exchangeName);
                return Ok(await exchange.GetPositionsAsync(_timeout));

            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }
    }
}
