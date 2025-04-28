using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using OpenShock.Common.Constants;

namespace OpenShock.Common.DataAnnotations;

/// <summary>
/// An attribute used to validate whether an email is valid.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ValidationAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class EmailAddressAttribute : ValidationAttribute
{
    /// <summary>
    /// Example value used to generate OpenApi documentation.
    /// </summary>
    private const string ExampleValue = "user@example.com";

    private const string ErrMsgCannotBeNull = "Email cannot be null";
    private const string ErrMsgMustBeString = "Email must be a string";
    private const string ErrMsgTooShort = "Email is too short";
    private const string ErrMsgTooLong = "Email is too long";
    private const string ErrMsgMustBeEmail = "Email must be an email address";

    /// <summary>
    /// Indicates whether validation should be performed.
    /// </summary>
    public bool ShouldValidate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddressAttribute"/> class with the specified validation behavior.
    /// </summary>
    /// <param name="shouldValidate">True if validation should be performed; otherwise, false.</param>
    public EmailAddressAttribute(bool shouldValidate) => ShouldValidate = shouldValidate;

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (!ShouldValidate) return ValidationResult.Success;

        if (value is null) return new ValidationResult(ErrMsgCannotBeNull);
        
        if (value is not string email) return new ValidationResult(ErrMsgMustBeString);
        
        if (email.Length < HardLimits.EmailAddressMinLength) return new ValidationResult(ErrMsgTooShort);
        
        if (email.Length > HardLimits.EmailAddressMaxLength) return new ValidationResult(ErrMsgTooLong);

        if (!MailAddress.TryCreate(email, out _)) return new ValidationResult(ErrMsgMustBeEmail);
        
        return ValidationResult.Success;
    }
}