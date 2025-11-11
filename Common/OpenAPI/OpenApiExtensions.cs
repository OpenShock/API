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
        builder.Services.AddOpenApi("v1", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
        });
        builder.Services.AddOpenApi("v2", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "2"));
        });
        builder.Services.AddOpenApi("internal", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
        });
        
        return builder.Services;
    }
}