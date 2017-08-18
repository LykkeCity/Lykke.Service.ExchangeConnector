using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers.Api
{
    [Route("api/isAlive")]
    public class IsAliveController : Controller
    {
        public bool Get()
        {
            return true;
        }
    }
}