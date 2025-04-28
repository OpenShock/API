using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace OpenShock.Common.OpenApi;

public sealed class OpenApiOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
