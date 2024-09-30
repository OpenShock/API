using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace OpenShock.ServicesCommon.Problems;

public sealed class ProducesDocAttribute : Attribute, IApiResponseMetadataProvider
{
    public void SetContentTypes(MediaTypeCollection contentTypes)
    {
        contentTypes.Add("application/json");
        contentTypes.Add("application/problem+json");
    }

    public Type? Type { get; }
    public int StatusCode { get; }
}