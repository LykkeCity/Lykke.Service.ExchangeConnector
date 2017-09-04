using System.Linq;
using Lykke.Common.ApiLibrary.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingBot.Communications;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            
            app.UseMiddleware<StatusCodeExceptionHandler>();
            
            app.UseMvcWithDefaultRoute();

            app.UseSwagger();
            app.UseSwaggerUi();

            app.Run(async (context) =>
            {
                var report = await StatusReport.Create();

                var response = $"Status: {(report.LastPrices.Any() ? "OK" : "FAIL")}\n\nLast prices:\n{string.Join("\n", report.LastPrices)}";
                
                await context.Response.WriteAsync(response);
            });
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "ExchangeConnectorAPI");
                options.OperationFilter<AddSwaggerAuthorizationHeaderParameter>();
            });

            services.AddSingleton(Program.Application);
            var provider = services.BuildServiceProvider();
        }
    }
}