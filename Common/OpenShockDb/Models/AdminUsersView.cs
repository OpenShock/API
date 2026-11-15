// We are in a view, no need to restrict lengths lol
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace OpenShock.Common.OpenShockDb;

public sealed class AdminUsersView
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public required PasswordHashingAlgorithm? PasswordHashType { get; init; }

    public required List<RoleType> Roles { get; set; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime? ActivatedAt { get; init; }

    public required Guid? CreatedByAutomationTokenId { get; init; }

    public required DateTime? DeactivatedAt { get; init; }

    public required Guid? DeactivatedByUserId { get; init; }

    public required int ApiTokenCount { get; init; }

    public required int PasswordResetCount { get; init; }

    public required int ShockerUserShareCount { get; init; }

    public required int ShockerPublicShareCount { get; init; }

    public required int EmailChangeRequestCount { get; init; }

    public required int NameChangeRequestCount { get; init; }

    public required int DeviceCount { get; init; }

    public required int ShockerCount { get; init; }

    public required int ShockerControlLogCount { get; init; }
}
