using Common.Log;
using Lykke.Service.ExchangeDataStore.Core.Domain.Ticks;
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
    public class TicksController : BaseApiController
    {
        private readonly IOrderBookService _orderBookService;
        private readonly ILog _log;
        public TicksController(IOrderBookService orderBookService, ILog log) : base(log)
        {
            _orderBookService = orderBookService;
            _log = log;
        }

        /// <summary>
        /// Get ticks
        /// <param name="request">The name of the exchange and instrument symbol</param>
        /// <param name="dateTimeFrom">Period from</param>
        /// <param name="dateTimeTo">Period to</param>
        /// </summary>
        [SwaggerOperation("GetTickPrices")]
        [HttpGet("{exchangeName}/{instrument}")]
        [ProducesResponseType(typeof(IEnumerable<TickPrice>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(OrderBookRequest request,  [FromQuery]DateTime dateTimeFrom, [FromQuery]DateTime? dateTimeTo = null)
        {
            try
            {
                return Ok(await _orderBookService.GetTickPricesAsync(request.ExchangeName, request.Instrument, dateTimeFrom, dateTimeTo ?? DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                return await LogAndReturnInternalServerError($"{request.ExchangeName}, {request.Instrument}, {dateTimeFrom}, {dateTimeTo}", ControllerContext, ex);
            }
        }

        

    }
}
