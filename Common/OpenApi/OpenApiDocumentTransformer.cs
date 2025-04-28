using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace OpenShock.Common.OpenApi;

public sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
