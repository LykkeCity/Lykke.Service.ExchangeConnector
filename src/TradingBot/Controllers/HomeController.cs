using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}