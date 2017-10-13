using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Models.Api;
using TradingBot.Trading;

namespace TradingBot.Controllers.Api
{
    public sealed class AccountController : BaseApiController
    {
        private readonly TimeSpan _timeout;

        public AccountController(ExchangeConnectorApplication app, AppSettings appSettings)
            : base(app)
        {
            _timeout = appSettings.AspNet.ApiTimeout;

        }

        /// <summary>
        /// Returns a simple balance for the exchange
        /// </summary>
        /// <param name="exchangeName">The exchange name</param>
        /// <returns></returns>
        [HttpGet("balance")]
        [ApiKeyAuth]
        [ProducesResponseType(typeof(IEnumerable<AccountBalance>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBalance([FromQuery]string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
            {
                return BadRequest($"Invalid {nameof(exchangeName)}");
            }
            try
            {
                return Ok(await Application.GetExchange(exchangeName).GetAccountBalance(CancellationToken.None));
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.BadRequest, e.Message, e);
            }
        }

        /// <summary>
        /// Returns full balance information on the exchange
        /// </summary>
        /// <param name="exchangeName">The exchange name</param>
        /// <returns></returns>
        [ApiKeyAuth]
        [HttpGet("tradeBalance")]
        [ProducesResponseType(typeof(TradeBalanceModel), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTradeBalance([FromQuery]string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
            {
                return BadRequest($"Invalid {nameof(exchangeName)}");
            }
            try
            {
                return Ok(await Application.GetExchange(exchangeName).GetTradeBalance(new CancellationTokenSource(_timeout).Token));
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.InternalServerError, e.Message, e);
            }
        }
    }
}
