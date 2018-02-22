using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TradingBot.Infrastructure.Auth
{
    public class AuthHandler : AuthenticationHandler<AuthOptions>
    {
        internal static string ApiKey { get; set; }

        private readonly HttpContext _httpContext;
        private readonly ILog _log;

        public AuthHandler(IOptionsMonitor<AuthOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock, IHttpContextAccessor httpContextAccessor,
            ILog log) : base(options, logger,
            encoder, clock)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
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
