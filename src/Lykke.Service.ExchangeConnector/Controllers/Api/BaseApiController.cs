using Microsoft.AspNetCore.Mvc;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [LoggingAspNetFilter]
    public abstract class BaseApiController : Controller
    {
        protected readonly IApplicationFacade Application;
        
        protected BaseApiController(IApplicationFacade app)
        {
            Application = app;
        }
    }
}
