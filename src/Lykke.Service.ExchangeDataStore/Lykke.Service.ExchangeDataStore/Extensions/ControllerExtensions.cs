using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.ExchangeDataStore.Extensions
{
    public static class ControllerExtensions
    {
        public static string GetControllerAndAction(this ControllerContext contContext)
        {
            return $"{contContext.RouteData.Values["controller"].ToString()}/{contContext.RouteData.Values["action"].ToString()}";
        }
    }
}
