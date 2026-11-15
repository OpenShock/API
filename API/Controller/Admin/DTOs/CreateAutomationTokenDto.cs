using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class CreateAutomationTokenDto : IValidatableObject
{
    [Required]
    [MinLength(1)]
    [MaxLength(ApiHardLimits.ApiKeyNameMaxLength)]
    public required string Name { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<AutomationTokenType> Types { get; init; }

    /// <summary>
    /// Whether the automated accounts this token creates are deleted once <see cref="AutoCleanupAfter"/> has
    /// passed since they were created. Off by default; accounts the token didn't create are never deleted.
    /// </summary>
    public bool AutoCleanupUsers { get; init; } = false;

    public TimeSpan? AutoCleanupAfter { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult(
                $"{nameof(Name)} must not be blank.",
                [nameof(Name)]);

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

public sealed class PatchAutomationTokenDto : IValidatableObject
{
    [MinLength(1)]
    [MaxLength(ApiHardLimits.ApiKeyNameMaxLength)]
    public string? Name { get; init; }

    [MinLength(1)]
    public IReadOnlyList<AutomationTokenType>? Types { get; init; }

    public bool? AutoCleanupUsers { get; init; }

    public TimeSpan? AutoCleanupAfter { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Name is not null && string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult(
                $"{nameof(Name)} must not be blank.",
                [nameof(Name)]);

        if (AutoCleanupAfter is { } d && d <= TimeSpan.Zero)
            yield return new ValidationResult(
                $"{nameof(AutoCleanupAfter)} must be positive.",
                [nameof(AutoCleanupAfter)]);
    }
}
