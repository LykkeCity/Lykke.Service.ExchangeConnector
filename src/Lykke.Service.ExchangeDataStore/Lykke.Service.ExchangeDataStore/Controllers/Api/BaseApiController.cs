using Common;
using Common.Log;
using Lykke.Service.ExchangeDataStore.Extensions;
using Lykke.Service.ExchangeDataStore.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [LoggingAspNetFilter]
    public class BaseApiController : Controller
    {
        private readonly ILog _log;
        private string TECHNICAL_ERROR_MESSAGE = "Error while processing request.";
        public static string BaseApiUrl = "api/v1";

        public BaseApiController(ILog log)
        {
            _log = log;
        }

        protected async Task<ObjectResult> LogAndReturnInternalServerError<T>(T callParams, ControllerContext controllerCtx, Exception ex)
        {
            await _log.WriteErrorAsync($"{BaseApiUrl}/{controllerCtx.GetControllerAndAction()}", new { callParams }.ToJson(), ex);
            return StatusCode((int)HttpStatusCode.InternalServerError, TECHNICAL_ERROR_MESSAGE);
        }
    }
}
