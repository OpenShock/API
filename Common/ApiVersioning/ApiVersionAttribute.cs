namespace OpenShock.Common.ApiVersioning;

/// <summary>
/// The API version an action is served under, filled into the <c>{version:apiVersion}</c> placeholder of its route:
/// <c>[HttpPost("login")]</c> on a controller routed at <c>/{version:apiVersion}/account</c> with <c>[ApiVersion(2)]</c> serves <c>/2/account/login</c>.
/// </summary>
/// <remarks>An action without it is not versioned, and its route may not contain the placeholder. See <see cref="ApiVersionRouteConvention"/>.</remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class ApiVersionAttribute : Attribute
{
    public ApiVersionAttribute(uint version)
    {
        ArgumentOutOfRangeException.ThrowIfZero(version);

        Version = version;
    }

    public uint Version { get; }
}
