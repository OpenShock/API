using System.Net;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;

namespace OpenShock.API.Models.Response;

public sealed class LoginSessionResponse
{
    public static LoginSessionResponse MapFrom(LoginSession session, ILogger logger)
    {
        return new LoginSessionResponse
        {
            Id = session.PublicId!.Value,
            Ip = IpAddressUtils.ParseStoredOrThrow(session.Ip, logger)!,
            UserAgent = session.UserAgent,
            Created = session.Created!.Value,
            Expires = session.Expires!.Value,
            LastUsed = session.LastUsed,
            AsnOrg = session.AsnOrg,
            IsVpn = session.IsVpn,
            CountryCode = session.CountryCode,
            City = session.City,
        };
    }

    public required Guid Id { get; init; }
    public required IPAddress Ip { get; init; }
    public required string UserAgent { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Expires { get; init; }
    public required DateTimeOffset? LastUsed { get; init; }
    public string? AsnOrg { get; init; }
    public bool? IsVpn { get; init; }
    public string? CountryCode { get; init; }
    public string? City { get; init; }
}
