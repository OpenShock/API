using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenAPI;

public static class DocumentDefaults
{
    public static Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> GetDocumentTransformer(string version)
    {
        return (document, context, _) =>
        {
            var env = context.ApplicationServices.GetRequiredService<IHostEnvironment>();
            
            document.Info = new OpenApiInfo
            {
                Title = "OpenShock API",
                // Summary = ...
                // Description = ...
                Version = version,
                // TermsOfService = ...
                // Contact = ...
                // License = ...
            };
            
            document.Servers =
            [
                new OpenApiServer { Url = "https://api.openshock.app" },
                new OpenApiServer { Url = "https://api.openshock.dev" }
            ];
            if (env.IsDevelopment())
            {
                document.Servers.Add(new OpenApiServer { Url = "https://localhost" });
            }
            
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                {
                    "ApiToken",
                    new OpenApiSecurityScheme
                    {
                        Name = "OpenShockToken",
                        Description = "Enter API Token",
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header
                    }
                },
                {
                    "HubToken",
                    new OpenApiSecurityScheme
                    {
                        Name = "DeviceToken",
                        Description = "Enter hub token",
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header
                    }
                },
                {
                    "UserSessionCookie",
                    new OpenApiSecurityScheme
                    {
                        Name = "openShockSession",
                        Description = "Enter user session cookie",
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Cookie
                    }
                }
            };
            
            return Task.CompletedTask;
        };
    }
}