namespace Lykke.ExternalExchangesApi.Exchanges.Abstractions.Models
{
    public class ReceivedHttpResponse
    {
        public string Content { get; set; }

        public ReceivedHttpResponse(string content)
        {
            Content = content;
        }
    }
}
