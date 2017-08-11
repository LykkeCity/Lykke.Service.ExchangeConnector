using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TradingBot.Infrastructure.Exceptions
{
    public class StatusCodeExceptionHandler
    {
        private readonly RequestDelegate request;

        public StatusCodeExceptionHandler(RequestDelegate next)
        {
            this.request = next;
        }

        public Task Invoke(HttpContext context) => this.InvokeAsync(context);
        
        async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.request(context);
            }
            catch (StatusCodeException e)
            {
                context.Response.StatusCode = (int) e.StatusCode;
                context.Response.Headers.Clear();
            }
        }
    }
}