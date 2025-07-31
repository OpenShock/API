using OpenShock.Common.Redis;

namespace OpenShock.API.Models.Response;

public sealed class LoginSessionResponse
{
    public static LoginSessionResponse MapFrom(LoginSession session)
    {
        return new LoginSessionResponse
        {
            Id = session.PublicId!.Value,
            Ip = session.Ip,
            UserAgent = session.UserAgent,
            Created = session.Created!.Value,
            Expires = session.Expires!.Value,
            LastUsed = session.LastUsed
        };
    }

    public required Guid Id { get; init; }
    public required string Ip { get; init; }
    public required string UserAgent { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Expires { get; init; }
    public required DateTimeOffset? LastUsed { get; init; }
}