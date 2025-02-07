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
    public sealed class AdminUserView_Hub
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
    public sealed class AdminUserView_ApiToken
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required List<PermissionType> Permissions { get; set; }
        public required DateTime? ValidUntil { get; set; }
        public required DateTime LastUsed { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required IPAddress CreatedByIp { get; set; }
    }
    public sealed class AdminUserView_PasswordReset
    {
        public required Guid Id { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime? UsedAt { get; set; }
    }
    public sealed class AdminUserView_UserActivation
    {
        public required Guid Id { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime? UsedAt { get; set; }
    }
    public sealed class AdminUserView_EmailChange
    {
        public required Guid Id { get; set; }
        public required string Email { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime? UsedAt { get; set; }
    }
    public sealed class AdminUserView_NameChange
    {
        public required int Id { get; set; }
        public required string OldName { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
    public sealed class AdminUserView
    {
        public required Guid Id { get; set; }

        public required string Name { get; set; }

        public required string Email { get; set; }

        public required PasswordHashingAlgorithm PasswordHashType { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required bool EmailActivated { get; set; }

        public required List<RoleType> Roles { get; set; }

        public required AdminUserView_Hub[] Hubs { get; set; }
        public required AdminUserView_ApiToken[] ApiTokens { get; set; }
        public required AdminUserView_NameChange[] UsersNameChanges { get; set; }
        public required AdminUserView_EmailChange[] UsersEmailChanges { get; set; }
        public required AdminUserView_PasswordReset[] PasswordResets { get; set; }
        public required AdminUserView_UserActivation[] UsersActivations { get; set; }
        public required int ShockerControlLogsCount { get; set; }
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
                u.CreatedAt,
                u.EmailActivated,
                u.Roles,
                Hubs = u.Devices.Select(hub =>
                    new AdminUserView_Hub
                    {
                        Id = hub.Id,
                        Name = hub.Name,
                        CreatedAt = hub.CreatedOn,
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
                        CreatedAt = token.CreatedOn,
                        CreatedByIp = token.CreatedByIp,
                    }
                ).ToArray(),
                PasswordResets = u.PasswordResets.Select(reset =>
                    new AdminUserView_PasswordReset
                    {
                        Id = reset.Id,
                        CreatedAt = reset.CreatedOn,
                        UsedAt = reset.UsedOn,
                    }
                ).ToArray(),
                UsersActivations = u.UsersActivations.Select(activation =>
                    new AdminUserView_UserActivation
                    {
                        Id = activation.Id,
                        CreatedAt = activation.CreatedOn,
                        UsedAt = activation.UsedOn,
                    }
                ).ToArray(),
                UsersEmailChanges = u.UsersEmailChanges.Select(change =>
                    new AdminUserView_EmailChange
                    {
                        Id = change.Id,
                        Email = change.Email,
                        CreatedAt = change.CreatedOn,
                        UsedAt = change.UsedOn,
                    }
                ).ToArray(),
                UsersNameChanges = u.UsersNameChanges.Select(change =>
                    new AdminUserView_NameChange
                    {
                        Id = change.Id,
                        OldName = change.OldName,
                        CreatedAt = change.CreatedOn,
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
            CreatedAt = user.CreatedAt,
            EmailActivated = user.EmailActivated,
            Roles = user.Roles,
            Hubs = user.Hubs,
            ApiTokens = user.ApiTokens,
            PasswordResets = user.PasswordResets,
            UsersActivations = user.UsersActivations,
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