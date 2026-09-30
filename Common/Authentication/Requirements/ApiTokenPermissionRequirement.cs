using Microsoft.AspNetCore.Authorization;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Authentication.Requirements;

public class ApiTokenPermissionRequirement : IAuthorizationRequirement
{
    public ApiTokenPermissionRequirement(PermissionType requiredPermission)
    {
        RequiredPermission = requiredPermission;
    }

    public PermissionType RequiredPermission { get; init; }
}
