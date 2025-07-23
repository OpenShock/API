using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mime;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    public sealed class AdminUserView_UserRef
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
    }
    public sealed class AdminUserView_Shocker
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }
    public sealed class AdminUserView_Hub
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required AdminUserView_Shocker[] Shockers { get; init; }
    }
    public sealed class AdminUserView_ApiToken
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required List<PermissionType> Permissions { get; init; }
        public required DateTimeOffset? ValidUntil { get; init; }
        public required DateTimeOffset LastUsed { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required IPAddress CreatedByIp { get; init; }
    }
    public sealed class AdminUserView_PasswordReset
    {
        public required Guid Id { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset? UsedAt { get; init; }
    }
    public sealed class AdminUserView_UserActivationRequest
    {
        public required int EmailSendAttempts { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }
    public sealed class AdminUserView_UserDeactivation
    {
        public required AdminUserView_UserRef DeactivatedBy { get; init; }
        public required DateTimeOffset? ScheduledDeletionTime { get; init; }
        public required DateTimeOffset DeactivatedAt { get; init; }
    }
    public sealed class AdminUserView_EmailChange
    {
        public required Guid Id { get; init; }
        public required string Email { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset? UsedAt { get; init; }
    }
    public sealed class AdminUserView_NameChange
    {
        public required int Id { get; init; }
        public required string OldName { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }
    public sealed class AdminUserView
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string Email { get; init; }

        public required PasswordHashingAlgorithm PasswordHashType { get; init; }

        public required List<RoleType> Roles { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        public required DateTimeOffset? ActivatedAt { get; init; }

        public required AdminUserView_UserActivationRequest? ActivationRequest { get; init; }

        public required AdminUserView_UserDeactivation? Deactivation { get; init; }

        public required AdminUserView_Hub[] Hubs { get; init; }
        public required AdminUserView_ApiToken[] ApiTokens { get; init; }
        public required AdminUserView_NameChange[] UsersNameChanges { get; init; }
        public required AdminUserView_EmailChange[] UsersEmailChanges { get; init; }
        public required AdminUserView_PasswordReset[] PasswordResets { get; init; }
        public required int ShockerControlLogsCount { get; init; }
    }

    [NonAction]
    private async Task<AdminUserView?> GetUserBySelectorAsync(Expression<Func<User,bool>> expression, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(expression)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.PasswordHash,
                u.Roles,
                u.CreatedAt,
                u.ActivatedAt,
                Hubs = u.Devices.Select(hub =>
                    new AdminUserView_Hub
                    {
                        Id = hub.Id,
                        Name = hub.Name,
                        CreatedAt = hub.CreatedAt,
                        Shockers = hub.Shockers.Select(shocker => new AdminUserView_Shocker
                        {
                            Id = shocker.Id,
                            Name = shocker.Name,
                            CreatedAt = shocker.CreatedAt,
                        }).ToArray()
                    }
                ).ToArray(),
                ApiTokens = u.ApiTokens.Select(token =>
                    new AdminUserView_ApiToken
                    {
                        Id = token.Id,
                        Name = token.Name,
                        Permissions = token.Permissions,
                        ValidUntil = token.ValidUntil,
                        LastUsed = token.LastUsed,
                        CreatedAt = token.CreatedAt,
                        CreatedByIp = token.CreatedByIp,
                    }
                ).ToArray(),
                PasswordResets = u.PasswordResets.Select(reset =>
                    new AdminUserView_PasswordReset
                    {
                        Id = reset.Id,
                        CreatedAt = reset.CreatedAt,
                        UsedAt = reset.UsedAt,
                    }
                ).ToArray(),
                UsersActivations = u.UserActivationRequest.Select(activation =>
                    new AdminUserView_UserActivationRequest
                    {
                        Id = activation.Id,
                        CreatedAt = activation.CreatedOn,
                        UsedAt = activation.UsedOn,
                    }
                ).ToArray(),
                UsersEmailChanges = u.EmailChanges.Select(change =>
                    new AdminUserView_EmailChange
                    {
                        Id = change.Id,
                        Email = change.Email,
                        CreatedAt = change.CreatedAt,
                        UsedAt = change.UsedAt,
                    }
                ).ToArray(),
                UsersNameChanges = u.NameChanges.Select(change =>
                    new AdminUserView_NameChange
                    {
                        Id = change.Id,
                        OldName = change.OldName,
                        CreatedAt = change.CreatedAt,
                    }
                ).ToArray(),
                ShockerControlLogsCount = u.ShockerControlLogs.Count()

            })
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null) return null;

        if (string.IsNullOrEmpty(user.PasswordHash) || !Enum.TryParse(user.PasswordHash, true, out PasswordHashingAlgorithm passwordHashingAlgorithm))
        {
            passwordHashingAlgorithm = PasswordHashingAlgorithm.Unknown;
        }

        return new AdminUserView
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PasswordHashType = passwordHashingAlgorithm,
            Roles = user.Roles,
            CreatedAt = user.CreatedAt,
            ActivatedAt = user.ActivatedAt,
            Hubs = user.Hubs,
            ApiTokens = user.ApiTokens,
            PasswordResets = user.PasswordResets,
            ActivationRequest = user.UsersActivations,
            UsersEmailChanges = user.UsersEmailChanges,
            UsersNameChanges = user.UsersNameChanges,
            ShockerControlLogsCount = user.ShockerControlLogsCount,
        };
    }


    /// <summary>
    /// Gets a user by id
    /// </summary>
    /// <response code="200">User info</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("users/{userId}")]
    [ProducesResponseType<AdminUserView>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById([FromRoute(Name = "userId")] Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserBySelectorAsync(u => u.Id == userId, cancellationToken);
        return user == null ? NotFound() : Ok(user);
    }
}