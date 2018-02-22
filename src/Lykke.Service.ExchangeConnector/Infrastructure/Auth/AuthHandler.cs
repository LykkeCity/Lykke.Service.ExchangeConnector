using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TradingBot.Models.Api;

namespace TradingBot.Infrastructure.Auth
{
    public class AuthHandler : AuthenticationHandler<AuthOptions>
    {
        internal static string ApiKey { get; set; }

        private readonly HttpContext httpContext;
        private readonly ILog log;

        public AuthHandler(IOptionsMonitor<AuthOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock, IHttpContextAccessor httpContextAccessor,
            ILog log) : base(options, logger,
            encoder, clock)
        {
            httpContext = httpContextAccessor.HttpContext;
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var stream = httpContext.Request.Body;
            var originalContent = new StreamReader(stream).ReadToEnd();

            OrderModel signedModel = null;
            try
            {
                signedModel = JsonConvert.DeserializeObject<OrderModel>(originalContent);
            }
            catch
            {

            }
            
            if (signedModel != null)
            {

            }

            //var temp = model;


            //var apikey = httpContext.Request.Headers[AuthConstants.Headers.ApiKeyHeaderName];

            //if (string.IsNullOrWhiteSpace(apikey))
            //    return AuthenticateResult.NoResult();

            //if (apikey != ApiKey)
            //    return AuthenticateResult.Fail("Invalid key");

            return CreateSuccessResult();
        }

        private AuthenticateResult CreateSuccessResult()
        {
            var identities = new List<ClaimsIdentity> { new ClaimsIdentity("Header") };

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identities), Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
