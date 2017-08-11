using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers.Api
{
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    public abstract class BaseApiController : Controller
    {
        protected IApplicationFacade Application;
        
        protected BaseApiController()
        {
            Application = Program.Application;
        }
    }
}