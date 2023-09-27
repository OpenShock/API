using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenShock.Common.Serialization;

public static class Options
{
    public static readonly DefaultContractResolver DefaultCamelCaseResolver = new()
    {
        NamingStrategy = new CamelCaseNamingStrategy()
    };

    public static readonly JsonSerializerSettings Default = new()
    {
        ContractResolver = DefaultCamelCaseResolver,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
}