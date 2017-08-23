using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Controllers.Api
{
    public class AccountController : BaseApiController
    {
        [HttpGet("{exchangeName}")]
        public Task<Dictionary<string, decimal>> GetBalance(string exchangeName)
        {
            try
            {
                //var cts = new CancellationTokenSource();
                return Application.GetExchange(exchangeName).GetAccountBalance(CancellationToken.None);
            }
            catch (Exception e)
            {
                throw new StatusCodeException(HttpStatusCode.BadRequest, e.Message, e);
            }
        }
    }
}