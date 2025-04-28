using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;

namespace OpenShock.Common.DataAnnotations;

/// <summary>
/// An attribute used to validate whether a password is valid.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ValidationAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PasswordAttribute : ValidationAttribute
{
    /// <summary>
    /// Example value used to generate OpenApi documentation.
    /// </summary>
    private const string ExampleValue = "user@example.com";

    private const string ErrMsgCannotBeNull = "Password cannot be null";
    private const string ErrMsgMustBeString = "Password must be a string";
    private const string ErrMsgTooShort = "Password is too short";
    private const string ErrMsgTooLong = "Password is too long";
    private const string ErrMsgCannotStartOrEndWithWhiteSpace = "Password cannot start or end with whitespace";

    /// <summary>
    /// Indicates whether validation should be performed.
    /// </summary>
    public bool ShouldValidate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordAttribute"/> class with the specified validation behavior.
    /// </summary>
    /// <param name="shouldValidate">True if validation should be performed; otherwise, false.</param>
    public PasswordAttribute(bool shouldValidate) => ShouldValidate = shouldValidate;

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (!ShouldValidate) return ValidationResult.Success;

        if (value is null) return new ValidationResult(ErrMsgCannotBeNull);
        
        if (value is not string password) return new ValidationResult(ErrMsgMustBeString);
        
        if (password.Length < HardLimits.EmailAddressMinLength) return new ValidationResult(ErrMsgTooShort);
        
        if (password.Length > HardLimits.EmailAddressMaxLength) return new ValidationResult(ErrMsgTooLong);

        if (password.Trim().Length != password.Length) return new ValidationResult(ErrMsgCannotStartOrEndWithWhiteSpace);
        
        return ValidationResult.Success;
    }
}