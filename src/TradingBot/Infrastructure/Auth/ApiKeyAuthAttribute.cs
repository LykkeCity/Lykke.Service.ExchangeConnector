using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Auth
{
    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        private readonly ILogger logger = Logging.Logging.CreateLogger<ApiKeyAuthAttribute>();

        private readonly string HeaderName = "X-ApiKey";

        internal static string ApiKey { get; set; }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey(HeaderName))
            {
                context.Result = new UnauthorizedResult();
                logger.LogDebug($"Unauthorized request for {context.HttpContext.Request.Method} {context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString} without {HeaderName} header");
            }
            else
            {
                var apiKeyFromRequest = context.HttpContext.Request.Headers[HeaderName];

                if (!ApiKey.Equals(apiKeyFromRequest))
                {
                    context.Result = new UnauthorizedResult();
                    logger.LogDebug($"Unauthorized request for {context.HttpContext.Request.Method} {context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString} with wrong api key: {apiKeyFromRequest}");
                }
            }
            
            
            base.OnActionExecuting(context);
        }
    }
}