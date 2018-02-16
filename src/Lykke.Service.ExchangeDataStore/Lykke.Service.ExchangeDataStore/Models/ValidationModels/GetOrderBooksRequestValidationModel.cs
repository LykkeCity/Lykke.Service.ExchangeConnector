using FluentValidation;
using Lykke.Service.ExchangeDataStore.Models.Requests;

namespace Lykke.Service.ExchangeDataStore.Models.ValidationModels
{
    // ReSharper disable once UnusedMember.Global - auto invoked by the framework
    public class GetOrderBooksRequestValidationModel : AbstractValidator<OrderBookRequest>
    {
        public GetOrderBooksRequestValidationModel()
        {
            RuleFor(r => r.ExchangeName).NotEmpty().WithMessage($"Invalid exchange name.");
            RuleFor(r => r.Instrument).NotEmpty().WithMessage($"Invalid instrument name.");
        }
    }
}
