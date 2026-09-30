using System.Reflection;
using Asp.Versioning.OpenApi.Transformers;
using Serilog;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// The XML documentation of the running host and of Common, loaded once per process and shared by every OpenAPI document.
/// Documentation is optional: when it cannot be loaded the documents are served without descriptions.
/// </summary>
internal sealed class OpenApiXmlDocumentation
{
    private static readonly Lazy<OpenApiXmlDocumentation?> Shared = new(Load);

    public required DocumentedXmlComments Comments { get; init; }
    public required XmlCommentsTransformer Transformer { get; init; }

    public static OpenApiXmlDocumentation? Get() => Shared.Value;

    private static OpenApiXmlDocumentation? Load()
    {
        try
        {
            // Documentation of whichever host is running (API/Cron/LiveControlGateway), plus the shared Common assembly
            var names = new[] { Assembly.GetEntryAssembly()?.GetName().Name, typeof(OpenApiXmlDocumentation).Assembly.GetName().Name }
                .OfType<string>()
                .Distinct();

            var paths = new List<string>();
            foreach (var path in names.Select(name => Path.Combine(AppContext.BaseDirectory, name + ".xml")))
            {
                if (File.Exists(path)) paths.Add(path);
                else Log.Warning("OpenAPI XML documentation file {Path} was not found, its descriptions are left out", path);
            }

            if (paths.Count == 0) return null;

            // The library only reads documentation from a file, so the merged copy exists just while it is loaded
            var filledPath = XmlCrefText.CreateFilledCopy(paths);
            try
            {
                return new OpenApiXmlDocumentation
                {
                    Comments = new DocumentedXmlComments(filledPath),
                    Transformer = new XmlCommentsTransformer(filledPath)
                };
            }
            finally
            {
                File.Delete(filledPath);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to load OpenAPI XML documentation, documents are served without descriptions");
            return null;
        }
    }
}
