using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Validation;

namespace OpenShock.Common.DataAnnotations;

/// <summary>
/// An attribute used to validate whether a display name is valid.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ValidationAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class UsernameAttribute : ValidationAttribute
{
    /// <summary>
    /// Example value used to generate OpenApi documentation.
    /// </summary>
    private const string ExampleValue = "String";

    private const string ErrMsgCannotBeNull = "Username cannot be null";
    private const string ErrMsgMustBeString = "Username must be a string";

    /// <summary>
    /// Indicates whether validation should be performed.
    /// </summary>
    public bool ShouldValidate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UsernameAttribute"/> class with the specified validation behavior.
    /// </summary>
    /// <param name="shouldValidate">True if validation should be performed; otherwise, false.</param>
    public UsernameAttribute(bool shouldValidate) => ShouldValidate = shouldValidate;

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (!ShouldValidate) return ValidationResult.Success;

        if (value is null) return new ValidationResult(ErrMsgCannotBeNull);
        
        if (value is not string displayName) return new ValidationResult(ErrMsgMustBeString);

        var result = UsernameValidator.Validate(displayName);
        
        return result.Match(
            _ => ValidationResult.Success,
            error => new ValidationResult($"{error.Type} - {error.Message}")
        );
    }
}