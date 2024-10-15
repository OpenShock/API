using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OneOf;

namespace OpenShock.Common.Authentication.Services;

public interface IUserReferenceService
{
    public OneOf<LoginSession, ApiToken>? AuthReference { get; set; }
}

public sealed class UserReferenceService : IUserReferenceService
{
    public OneOf<LoginSession, ApiToken>? AuthReference { get; set; } = null;
}