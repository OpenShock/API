using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Results;

namespace OpenShock.Common.Authentication.Services;

public interface IUserReferenceService
{
    public Union3<LoginSession, ApiToken, None> AuthReference { get; set; }
}

public sealed class UserReferenceService : IUserReferenceService
{
    public Union3<LoginSession, ApiToken, None> AuthReference { get; set; } = new None();
}