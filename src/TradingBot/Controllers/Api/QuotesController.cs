using System;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers.Api
{
    public class QuotesController : BaseApiController
    {
        [HttpGet("{exchangeName}/{instrument}")]
        public IActionResult Index(string exchangeName, string instrument)
        {
            throw new NotImplementedException();
        }
    }
}