using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public class LoggingAspNetFilter : ActionFilterAttribute
    {
        private readonly ILogger logger = Logging.CreateLogger<LoggingAspNetFilter>();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            logger.LogInformation($"Action {context.ActionDescriptor.DisplayName} executing on controller {context.Controller.GetType()}, query string: {context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}");
            
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            logger.LogInformation($"Action {context.ActionDescriptor.DisplayName} executed on controller {context.Controller.GetType()}");
            
            base.OnActionExecuted(context);
        }
    }
}