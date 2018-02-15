using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.ExchangeDataStore.Models.Requests
{
    public class GetOrderBooksRequest
    {
        [FromRoute]
        public string ExchangeName { get; set; }
        [FromRoute]
        public string Instrument { get; set; }
    }
}
