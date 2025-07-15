using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenShock.Common.Constants;
using OpenShock.Common.DataAnnotations.Interfaces;

namespace OpenShock.Common.DataAnnotations;

/// <summary>
/// An attribute used to validate whether an email is valid.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ValidationAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class EmailAddressAttribute : ValidationAttribute, IParameterAttribute
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
    private const string ErrMsgNoAlias = "Email address cannot have an alias";
    private const string ErrMsgNoDisplayName = "Email address cannot have display name";

    /// <summary>
    /// Indicates whether validation should be performed.
    /// </summary>
    private bool ShouldValidate { get; }
    private bool CanContainAlias { get; }
    private bool CanContainDisplayName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddressAttribute"/> class with the specified validation behavior.
    /// </summary>
    /// <param name="shouldValidate">True if validation should be performed; otherwise, false.</param>
    /// <param name="canContainAlias">True if the email address can contain an alias; otherwise, false.</param>
    /// <param name="canContainDisplay">True if the email address can contain a display name; otherwise, false.</param>
    public EmailAddressAttribute(bool shouldValidate, bool canContainAlias, bool canContainDisplay)
    {
        ShouldValidate = shouldValidate;
        CanContainAlias = canContainAlias;
        CanContainDisplayName = canContainDisplay;
    }

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (!ShouldValidate) return ValidationResult.Success;

        if (value is null) return new ValidationResult(ErrMsgCannotBeNull);
        
        if (value is not string email) return new ValidationResult(ErrMsgMustBeString);
        
        if (email.Length < HardLimits.EmailAddressMinLength) return new ValidationResult(ErrMsgTooShort);
        
        if (email.Length > HardLimits.EmailAddressMaxLength) return new ValidationResult(ErrMsgTooLong);

        if (!MailAddress.TryCreate(email, out var parsed)) return new ValidationResult(ErrMsgMustBeEmail);
        
        if (!string.IsNullOrEmpty(parsed.DisplayName)) return new ValidationResult(ErrMsgNoDisplayName);
        
        if (parsed.User.Contains('+')) return new ValidationResult(ErrMsgNoAlias);
        
        return ValidationResult.Success;
    }

    /// <inheritdoc/>
    public void Apply(OpenApiSchema schema)
    {
        //if (ShouldValidate) schema.Pattern = ???;
        
        schema.Example = new OpenApiString(ExampleValue);
    }

    /// <inheritdoc/>
    public void Apply(OpenApiParameter parameter) => Apply(parameter.Schema);
}