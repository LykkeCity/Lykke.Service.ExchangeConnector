using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.SwaggerGen;
using TradingBot.Models;

namespace TradingBot.Controllers.Api
{
    /// <summary>
    /// Controller to test service is alive.
    /// </summary>
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        /// <summary>
        /// Checks service is alive
        /// </summary>
        [HttpGet]
        [SwaggerOperation("IsAlive")]
        [Produces("application/json", Type = typeof(IsAliveResponseModel))]
        public IsAliveResponseModel Get()
        {
            return new IsAliveResponseModel
            {
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Env = Environment.GetEnvironmentVariable("ENV_INFO") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            };
        }
    }
}
