using Microsoft.OpenApi;

namespace OpenShock.Common.OpenAPI;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiExt<TProgram>(this IServiceCollection services) where TProgram : class
    {
        var assembly = typeof(TProgram).Assembly;

        string assemblyName = assembly
                                .GetName()
                                .Name ?? throw new NullReferenceException("Assembly name");

        services.AddOutputCache(options =>
        {
            options.AddPolicy("OpenAPI", policy => policy.Expire(TimeSpan.FromMinutes(10)));
        });
        services.AddOpenApi("v1", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
        });
        services.AddOpenApi("v2", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "2"));
        });
        services.AddOpenApi("internal", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
        });
        
        return services;
    }
}