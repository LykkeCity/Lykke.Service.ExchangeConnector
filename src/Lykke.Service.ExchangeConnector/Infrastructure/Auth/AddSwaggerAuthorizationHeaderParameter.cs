using System.Collections.Generic;
using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TradingBot.Infrastructure.Auth
{
    public class AddSwaggerAuthorizationHeaderParameter : IOperationFilter
    {
        void IOperationFilter.Apply(Operation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var isAuthorized = filterPipeline.Select(f => f.Filter).Any(f => f is ApiKeyAuthAttribute);
            var authorizationRequired = context.ApiDescription.ControllerAttributes().Any(a => a is ApiKeyAuthAttribute);
            if (!authorizationRequired) authorizationRequired = context.ApiDescription.ActionAttributes().Any(a => a is ApiKeyAuthAttribute);

            if (isAuthorized && authorizationRequired)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<IParameter>();

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "X-ApiKey",
                    In = "header",
                    Description = "API key",
                    Required = true,
                    Type = "string"
                });
            }
        }
    }
}
