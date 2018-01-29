using System;
using Autofac;
using Common.Log;

namespace Lykke.Service.ExchangeDataStore.Client
{
    public static class AutofacExtension
    {
        public static void RegisterExchangeDataStoreClient(this ContainerBuilder builder, string serviceUrl, ILog log)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceUrl == null) throw new ArgumentNullException(nameof(serviceUrl));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.RegisterType<ExchangeDataStoreClient>()
                .WithParameter("serviceUrl", serviceUrl)
                .As<IExchangeDataStoreClient>()
                .SingleInstance();
        }

        public static void RegisterExchangeDataStoreClient(this ContainerBuilder builder, ExchangeDataStoreServiceClientSettings settings, ILog log)
        {
            builder.RegisterExchangeDataStoreClient(settings?.ServiceUrl, log);
        }
    }
}
