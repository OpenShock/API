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

    public required Guid Id { get; set; }
    public required string Ip { get; set; }
    public required string UserAgent { get; set; }
    public required DateTimeOffset Created { get; set; }
    public required DateTimeOffset Expires { get; set; }
    public required DateTimeOffset? LastUsed { get; set; }
}