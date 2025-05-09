using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace OpenShock.Common.OpenApi;

public sealed class OpenApiOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            throw new NotImplementedException();
        }
        
        operation.OperationId = actionDescriptor.ControllerName + actionDescriptor.ActionName;
        
        return Task.CompletedTask;
    }
}
