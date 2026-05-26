using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class BypassTokenDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<BypassTokenType> Types { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public Guid? LastUsedByUserId { get; init; }
    public DateTime? LastRotatedAt { get; init; }
    public long UseCount { get; init; }
    public bool AutoCleanupUsers { get; init; }
    public TimeSpan? AutoCleanupAfter { get; init; }
}

public sealed class CreatedBypassTokenDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Secret { get; init; }
    public required IReadOnlyList<BypassTokenType> Types { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastRotatedAt { get; init; }
    public bool AutoCleanupUsers { get; init; }
    public TimeSpan? AutoCleanupAfter { get; init; }
}
