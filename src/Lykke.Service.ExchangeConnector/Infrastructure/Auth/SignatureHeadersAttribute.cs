using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace TradingBot.Infrastructure.Auth
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class SignatureHeadersAttribute : ActionFilterAttribute
    {

    }
}
