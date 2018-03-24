using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers.Api
{
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : Controller
    {
        protected readonly IApplicationFacade Application;
        
        protected BaseApiController(IApplicationFacade app)
        {
            Application = app;
        }
    }
}
