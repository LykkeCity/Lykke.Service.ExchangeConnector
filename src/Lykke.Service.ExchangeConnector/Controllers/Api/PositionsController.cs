using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lykke.ExternalExchangesApi.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
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
        [SwaggerOperation("GetOpenedPosition")]
        [HttpGet]
        public Task<IReadOnlyCollection<PositionModel>> Index([FromQuery, Required] string exchangeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(exchangeName))
                {
                    throw new StatusCodeException(HttpStatusCode.InternalServerError, $"Invalid {nameof(exchangeName)}");
                }
                var exchange = Application.GetExchange(exchangeName);
                return exchange.GetPositions(_timeout);

            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }
    }
}
