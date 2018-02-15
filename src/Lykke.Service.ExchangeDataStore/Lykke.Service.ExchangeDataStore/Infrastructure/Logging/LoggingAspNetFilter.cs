using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Lykke.Service.ExchangeDataStore.Infrastructure.Logging
{
    public class LoggingAspNetFilter : ActionFilterAttribute
    {
        private readonly ILogger _logger = Logging.CreateLogger<LoggingAspNetFilter>();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogInformation($"Action {context.ActionDescriptor.DisplayName} executing on controller {context.Controller.GetType()}, query string: {context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}");
            
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _logger.LogInformation($"Action {context.ActionDescriptor.DisplayName} executed on controller {context.Controller.GetType()}");
            
            base.OnActionExecuted(context);
        }
    }
}
