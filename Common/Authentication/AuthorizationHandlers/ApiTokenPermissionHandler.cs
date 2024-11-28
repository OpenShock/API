using Microsoft.AspNetCore.Authorization;
using OpenShock.Common.Authentication.Requirements;
using OpenShock.Common.Models;

namespace OpenShock.Common.Authentication.AuthorizationHandlers;

public class ApiTokenPermissionHandler : AuthorizationHandler<ApiTokenPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiTokenPermissionRequirement requirement)
    {
        var perms = context
            .User
            .Identities
            .SelectMany(ident => ident.Claims.Where(claim => claim.Type == OpenShockAuthClaims.ApiTokenPermission))
            .Select(claim => Enum.Parse<PermissionType>(claim.Value));

        if (!requirement.RequiredPermission.IsAllowed(perms))
        {
            context.Fail(new AuthorizationFailureReason(this, $"You do not have the required permission to access this endpoint. Missing permission: {requirement.RequiredPermission}"));

            return Task.CompletedTask;
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
