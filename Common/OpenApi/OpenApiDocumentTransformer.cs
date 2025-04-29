using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;

namespace OpenShock.Common.OpenApi;

public sealed class OpenApiDocumentTransformer<TProgram> : IOpenApiDocumentTransformer where TProgram : class
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Servers = [
#if DEBUG
            new OpenApiServer { Url = "https://localhost" },
#endif
            new OpenApiServer { Url = "https://api.openshock.app" },
            new OpenApiServer { Url = "https://api.openshock.dev" }
        ];

        return Task.CompletedTask;
    }
}
