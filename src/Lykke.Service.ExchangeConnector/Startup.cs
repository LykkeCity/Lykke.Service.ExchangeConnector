using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Linq;
using TradingBot.Communications;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Auth;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Modules;
using TradingBot.Repositories;

namespace TradingBot
{
    internal sealed class Startup
    {
        private IConfigurationRoot Configuration { get; }
        private IContainer ApplicationContainer { get; set; }
        private ILog _log;


        private INoSQLTableStorage<PriceTableEntity> _pricesStorage;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            //app.UseLykkeForwardedHeaders();
            app.UseLykkeMiddleware("LykkeService", ex => new { Message = "Technical problem" });

            app.UseMvc();
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
            });

            app.UseSwaggerUI(x =>
            {
                x.RoutePrefix = "swagger/ui";
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });

            // Dispose resources that have been resolved in the application container
            appLifetime.ApplicationStarted.Register(StartHandler);
            appLifetime.ApplicationStopping.Register(ShutDownHandler);
            appLifetime.ApplicationStopped.Register(() => ApplicationContainer.Dispose());

            app.Run(async (context) =>
            {
                // TODO: get for all enabled exchanges
                var report = await StatusReport.Create(_pricesStorage);

                var response = $"Status: {(report.LastPrices.Any() ? "OK" : "FAIL")}\n\nLast prices:\n{string.Join("\n", report.LastPrices)}";

                await context.Response.WriteAsync(response);
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ILog log = new LogToConsole();
            var appName = nameof(TradingBot);
            // Register dependencies, populate the services from
            // the collection, and build the container. If you want
            // to dispose of the container at the end of the app,
            // be sure to keep a reference to it as a property or field.
            var builder = new ContainerBuilder();

            try
            {
                // Add framework services.
                services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "ExchangeConnectorAPI");
                    //options.AddSecurityDefinition("CustomScheme", new ApiKeyScheme { In = "header", Description = "Please insert API key into field", Name = ApiKeyAuthAttribute.HeaderName, Type = "apiKey" });
                    options.OperationFilter<HeaderAccessOperationFilter>();
                });

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = AuthConstants.AuthenticationScheme;
                        options.DefaultChallengeScheme = AuthConstants.AuthenticationScheme;
                    })
                    .AddScheme<AuthOptions, AuthHandler>(AuthConstants.AuthenticationScheme,
                        AuthConstants.AuthenticationScheme, options => { });

                var settingsManager = Configuration.LoadSettings<TradingBotSettings>("SettingsUrl");

                builder.RegisterInstance(settingsManager)
                    .As<IReloadingManager<TradingBotSettings>>();

                var topSettings = settingsManager.CurrentValue;
                var settings = topSettings.TradingBot;
                builder.RegisterInstance(settings)
                    .As<AppSettings>()
                    .SingleInstance();


                if (settings.AzureStorage.Enabled)
                {
                    var slackService = services.UseSlackNotificationsSenderViaAzureQueue(topSettings.SlackNotifications.AzureQueue, log);
                    var tableStorage = AzureTableStorage<LogEntity>.Create(
                        settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.LogsConnString), settings.AzureStorage.LogTableName, log);
                    builder.RegisterInstance(tableStorage).As<INoSQLTableStorage<LogEntity>>().SingleInstance();
                    var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(appName, tableStorage, log);
                    var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(appName, slackService, log);
                    var logToTable = new LykkeLogToAzureStorage(
                        appName,
                        persistenceManager,
                        slackNotificationsManager,
                        log);
                    logToTable.Start();

                    log = new LogAggregate()
                        .AddLogger(log)
                        .AddLogger(logToTable)
                        .CreateLogger();
                }

                //ApiKeyAuthAttribute.ApiKey = settings.AspNet.ApiKey;
                //   SignedModelAuthAttribute.ApiKey = settings.AspNet.ApiKey; //TODO use it somewhere
                AuthHandler.ApiKey = settings.AspNet.ApiKey;


                builder.RegisterInstance(log).As<ILog>().SingleInstance();

                _pricesStorage = AzureTableStorage<PriceTableEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.EntitiesConnString), "tickPrices", log);

                var fixMessagesStorage = AzureTableStorage<FixMessageTableEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.EntitiesConnString), "fixMessages", log);
                builder.RegisterInstance(fixMessagesStorage).As<INoSQLTableStorage<FixMessageTableEntity>>().SingleInstance();

                var signalsStorage = AzureTableStorage<TranslatedSignalTableEntity>.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.EntitiesConnString), settings.AzureStorage.TranslatedSignalsTableName, log);
                builder.RegisterInstance(signalsStorage).As<INoSQLTableStorage<TranslatedSignalTableEntity>>().SingleInstance();

                var azureBlobStorage = AzureBlobStorage.Create(
                    settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.EntitiesConnString));
                builder.RegisterInstance(azureBlobStorage).As<IBlobStorage>().SingleInstance();

                builder.RegisterModule(new ServiceModule(settings, log));

                builder.Populate(services);


                ApplicationContainer = builder.Build();

                _log = log;
                // Create the IServiceProvider based on the container.
                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception exc)
            {
                log.WriteErrorAsync(
                    appName,
                    nameof(Startup),
                    nameof(ConfigureServices),
                    exc,
                    DateTime.UtcNow)
                   .Wait();
                throw;
            }
        }

        private void StartHandler()
        {
            ApplicationContainer.Resolve<IApplicationFacade>().Start().GetAwaiter().GetResult();
            _log.WriteMonitorAsync("", "", "Started").GetAwaiter().GetResult();

        }

        private void ShutDownHandler()
        {
            _log?.WriteMonitorAsync("", "", "Terminating").GetAwaiter().GetResult();
            try
            {
                // NOTE: Service can't recieve and process requests here, so you can destroy all resources

                ApplicationContainer.Resolve<IApplicationFacade>().Stop();
                ApplicationContainer.Resolve<TradingSignalsHandler>().Stop();

            }
            catch (Exception ex)
            {
                _log?.WriteFatalErrorAsync(nameof(Startup), nameof(ShutDownHandler), "", ex);
                throw;
            }
        }
    }
}
