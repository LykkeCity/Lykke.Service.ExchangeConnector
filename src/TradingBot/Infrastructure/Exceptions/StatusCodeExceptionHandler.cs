using System;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using TradingBot.Models;

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
                context.Response.Clear();
                context.Response.StatusCode = (int) e.StatusCode;
                context.Response.Headers.Clear();
                
                if (!context.Request.Headers.ContainsKey("Content-Type"))
                    context.Request.Headers.Add("Content-Type", "application/json");
                
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new ResponseMessage(e.Message, e.Model)));
            }
        }
    }
}