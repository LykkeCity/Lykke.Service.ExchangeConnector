using System;

namespace Lykke.Service.ExchangeConnector.Client
{
    public sealed partial class ExchangeConnectorService
    {
        public ExchangeConnectorService(ExchangeConnectorServiceSettings settings) : this(new Uri(settings.ServiceUrl), new ExchangeConnectorClientCredentials(settings.ApiKey))
        {

        }
    }

    /// <summary>
    /// A client for the ExchangeConnector service
    /// </summary>
    public partial interface IExchangeConnectorService
    {

    }

}
