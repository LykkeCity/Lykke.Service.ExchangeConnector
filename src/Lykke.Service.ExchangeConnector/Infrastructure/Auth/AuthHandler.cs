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
        //private readonly ISignatureVerificationService _signatureVerificationService;
        private readonly ILog _log;

        public AuthHandler(IOptionsMonitor<AuthOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock, IHttpContextAccessor httpContextAccessor,
            //ISignatureVerificationService signatureVerificationService,
            ILog log) : base(options, logger,
            encoder, clock)
        {
            _httpContext = httpContextAccessor.HttpContext;
            //_signatureVerificationService = signatureVerificationService ??
            //                                throw new ArgumentNullException(nameof(signatureVerificationService));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            //string merchantId = _httpContext.Request.GetMerchantId();
            //string merchantSign = _httpContext.Request.GetMerchantSign();

            //if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(merchantSign))
            //    return AuthenticateResult.NoResult();

            //try
            //{
            //    SecurityErrorType verificationResult =
            //        await _signatureVerificationService.VerifyRequest(_httpContext.Request);

            //    switch (verificationResult)
            //    {
            //        case SecurityErrorType.Ok:
            //            return CreateSuccessResult();
            //        case SecurityErrorType.SignIncorrect:
            //            return AuthenticateResult.Fail("Invalid signature");
            //        default:
            //            return AuthenticateResult.Fail("Unexpected signature verification result");
            //    }
            //}
            //catch (UnrecognizedSignatureVerificationException ex)
            //{
            //    await _log.WriteErrorAsync(nameof(LykkePayAuthHandler), nameof(HandleAuthenticateAsync), ex);

            //    return AuthenticateResult.Fail(ex.Message);
            //}
            //catch (Exception ex)
            //{
            //    await _log.WriteErrorAsync(nameof(LykkePayAuthHandler), nameof(HandleAuthenticateAsync), ex);

            //    return AuthenticateResult.Fail(ex);
            //}

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
