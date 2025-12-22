using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenAPI;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiExt<TProgram>(this WebApplicationBuilder builder) where TProgram : class
    {
        var assembly = typeof(TProgram).Assembly;

        string assemblyName = assembly
                                .GetName()
                                .Name ?? throw new NullReferenceException("Assembly name");

        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("OpenAPI", policy => policy.Expire(TimeSpan.FromMinutes(10)));
        });

        using (var tempProvider = builder.Services.BuildServiceProvider())
        {
            var apiVersionProvider = tempProvider.GetRequiredService<IApiVersionDescriptionProvider>();

            // Configure OpenAPI for each API version
            foreach (var description in apiVersionProvider.ApiVersionDescriptions)
            {
                builder.Services.AddOpenApi(description.GroupName, options =>
                {
                    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
                    options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(
                        version: description.ApiVersion.ToString()));
                });
            }
        }

        builder.Services.AddOpenApi("oauth", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.ShouldInclude = apiDescription => apiDescription.GroupName is "oauth";
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
        });
        builder.Services.AddOpenApi("admin", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.ShouldInclude = apiDescription => apiDescription.GroupName is "admin";
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
        });

        return builder.Services;
    }
}