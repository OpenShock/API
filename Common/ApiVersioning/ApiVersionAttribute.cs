namespace OpenShock.Common.ApiVersioning;

/// <summary>
/// The API version an action is served under, filled into the <c>{version:apiVersion}</c> placeholder of its route:
/// <c>[HttpPost("login")]</c> on a controller routed at <c>/{version:apiVersion}/account</c> with <c>[ApiVersion(2)]</c> serves <c>/2/account/login</c>.
/// </summary>
/// <remarks>
/// Belongs on the action method. On a controller it applies to all of that controller's actions, which is only meant for controllers whose
/// actions are inherited (the websocket controllers); an attribute on the method takes precedence. An action without a version is not
/// versioned, and its route may not contain the placeholder. See <see cref="ApiVersionRouteConvention"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ApiVersionAttribute : Attribute
{
    public ApiVersionAttribute(uint version)
    {
        ArgumentOutOfRangeException.ThrowIfZero(version);

        Version = version;
    }

    public uint Version { get; }
}
