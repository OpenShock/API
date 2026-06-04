using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OneOf;
using OneOf.Types;

namespace OpenShock.Common.Authentication.Services;

public interface IUserReferenceService
{
    public OneOf<LoginSession, ApiToken, None> AuthReference { get; set; }
}

public sealed class UserReferenceService : IUserReferenceService
{
    public OneOf<LoginSession, ApiToken, None> AuthReference { get; set; } = new None();
}