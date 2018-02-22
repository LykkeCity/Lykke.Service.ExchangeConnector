using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;
using System.Linq;

namespace TradingBot.Infrastructure.Auth
{
    public class HeaderAccessOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var isSignAccess = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is SignatureHeadersAttribute);
            if (isSignAccess)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<IParameter>();

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = AuthConstants.Headers.ApiKeyHeaderName,
                    In = "header",
                    Description = "X-ApiKey",
                    Required = true,
                    Type = "string"
                });
            }
        }
    }
}
