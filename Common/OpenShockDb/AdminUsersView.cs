using OpenShock.Common.Models;
// We are in a view, no need to restrict lengths lol
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace OpenShock.Common.OpenShockDb;

public class AdminUsersView
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public required PasswordHashingAlgorithm PasswordHashType { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required bool EmailActivated { get; set; }

    public required List<RoleType> Roles { get; set; }

    public required int ApiTokenCount { get; set; }

    public required int PasswordResetCount { get; set; }

    public required int ShockerShareCount { get; set; }

    public required int ShockerShareLinkCount { get; set; }

    public required int EmailChangeRequestCount { get; set; }

    public required int NameChangeRequestCount { get; set; }

    public required int UserActivationCount { get; set; }

    public required int DeviceCount { get; set; }

    public required int ShockerCount { get; set; }

    public required int ShockerControlLogCount { get; set; }
}
