using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class CreateBypassTokenDto : IValidatableObject
{
    [Required]
    [MinLength(1)]
    [MaxLength(HardLimits.ApiKeyNameMaxLength)]
    public required string Name { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<BypassTokenType> Types { get; init; }

    public bool AutoCleanupUsers { get; init; }

    public TimeSpan? AutoCleanupAfter { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (AutoCleanupUsers && AutoCleanupAfter is null)
            yield return new ValidationResult(
                $"{nameof(AutoCleanupAfter)} is required when {nameof(AutoCleanupUsers)} is true.",
                [nameof(AutoCleanupAfter)]);

        if (AutoCleanupAfter is { } d && d <= TimeSpan.Zero)
            yield return new ValidationResult(
                $"{nameof(AutoCleanupAfter)} must be positive.",
                [nameof(AutoCleanupAfter)]);
    }
}

public sealed class PatchBypassTokenDto
{
    [MaxLength(HardLimits.ApiKeyNameMaxLength)]
    public string? Name { get; init; }

    public IReadOnlyList<BypassTokenType>? Types { get; init; }

    public bool? AutoCleanupUsers { get; init; }

    public TimeSpan? AutoCleanupAfter { get; init; }
}
