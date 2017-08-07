using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TradingBot.Models.Api;

namespace TradingBot.Infrastructure.Auth
{
    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var model = context.ActionArguments.Values.FirstOrDefault(x => x is ISignedModel);

            if (model != null)
            {
                bool signIsCorrect = true;
                
                string stringToSign = ((ISignedModel) model).GetStringToSign();
                string modelSig = context.HttpContext.Request.Headers["authorization"];

                if (string.IsNullOrEmpty(modelSig))
                {
                    signIsCorrect = false;
                }
                else
                {
                    try
                    {
                        var apiKey = Configuration.Configuration.Instance.AspNet.ApiKey;

                        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey)))
                        {
                            var correctSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                            var sign = Convert.ToBase64String(correctSig); // for debug
                            var sigBytes = Convert.FromBase64String(modelSig);

                            if (correctSig.Length != sigBytes.Length)
                            {
                                signIsCorrect = false;
                            }
                            else
                            {
                                for (int i = 0; i < correctSig.Length; i++)
                                {
                                    if (correctSig[i] != sigBytes[i])
                                    {
                                        signIsCorrect = false;
                                    }
                                }    
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        signIsCorrect = false;
                    }
                }
                
                if (!signIsCorrect)
                    context.Result = new UnauthorizedResult();
            }
            
            base.OnActionExecuting(context);
        }
    }
}