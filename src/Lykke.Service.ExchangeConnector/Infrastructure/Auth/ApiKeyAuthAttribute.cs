using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TradingBot.Infrastructure.Auth
{
    public sealed class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        public const string HeaderName = "X-ApiKey";

        internal static string ApiKey { get; set; }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey(HeaderName))
            {
                context.Result = new BadRequestObjectResult($"No {HeaderName} header");
            }
            else
            {
                var apiKeyFromRequest = context.HttpContext.Request.Headers[HeaderName];

                if (!ApiKey.Equals(apiKeyFromRequest))
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            base.OnActionExecuting(context);
        }
    }
}
