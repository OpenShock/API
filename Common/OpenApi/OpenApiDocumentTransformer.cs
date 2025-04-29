using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;

namespace OpenShock.Common.OpenApi;

public sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "OpenShock API",
            Description = "Test description of API",
            Version = "v" + context.DocumentName,
            TermsOfService = new Uri("https://github.com/OpenShock/"),
            Contact = new OpenApiContact
            {
                Name = "Support",
                Url = new Uri("mailto:support@openshock.app"),
                Email = "support@openshock.app"
            },
            License = new OpenApiLicense
            {
                Name = "GNU affero General Public License v3.0",
                Url = new Uri("https://github.com/OpenShock/API/blob/develop/LICENSE")
            }
        };
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
