using Common.Log;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Services.OrderBooks;
using Lykke.Service.ExchangeDataStore.Models.Requests;
using Lykke.Service.ExchangeDataStore.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Controllers.Api
{
    [ValidateModel]
    public class OrderBooksController : BaseApiController
    {
        private readonly IOrderBookService _orderBookService;
        private readonly ILog _log;
        public OrderBooksController(IOrderBookService orderBookService, ILog log) : base(log)
        {
            _orderBookService = orderBookService;
            _log = log;
        }

        /// <summary>
        /// Get list of order books
        /// <param name="request">The name of the exchange and instrument symbol</param>
        /// <param name="dateTimeFrom">Period from</param>
        /// <param name="dateTimeTo">Period to</param>
        /// </summary>
        [SwaggerOperation("GetOrderBooks")]
        [HttpGet("{exchangeName}/{instrument}")]
        [ProducesResponseType(typeof(IEnumerable<OrderBook>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(GetOrderBooksRequest request, [FromQuery]DateTime dateTimeFrom, [FromQuery]DateTime? dateTimeTo = null)
        {
            try
            {
                return Ok(await _orderBookService.GetAsync(request.ExchangeName, request.Instrument, dateTimeFrom, dateTimeTo ?? DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                return await LogAndReturnInternalServerError($"{request.ExchangeName}, {request.Instrument}, {dateTimeFrom}, {dateTimeTo}", ControllerContext, ex);
            }
        }

    }
}
