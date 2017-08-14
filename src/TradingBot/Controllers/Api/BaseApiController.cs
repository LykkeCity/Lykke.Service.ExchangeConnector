using Microsoft.AspNetCore.Mvc;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Controllers.Api
{
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [LoggingAspNetFilter]
    public abstract class BaseApiController : Controller
    {
        protected IApplicationFacade Application;
        
        protected BaseApiController()
        {
            Application = Program.Application;
        }
    }
}