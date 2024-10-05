using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenShock.Common.DataAnnotations.Interfaces;
using OpenShock.Common.Validation;

namespace OpenShock.Common.DataAnnotations;

/// <summary>
/// An attribute used to validate whether a display name is valid.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ValidationAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class UsernameAttribute : ValidationAttribute, IParameterAttribute
{
    /// <summary>
    /// Regular expression for username validation.
    /// </summary>
    public const string DisplaynameRegex = /* lang=regex */ @"^[^\s].*[^\s]$";

    /// <summary>
    /// Example username used to generate OpenApi documentation.
    /// </summary>
    public const string ExampleDisplayname = "String";

    private const string _ErrMsgMustBeString = "Must be a string";

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

        if (value is null) return new ValidationResult("Username cannot be null");
        
        if (value is not string displayname) return new ValidationResult(_ErrMsgMustBeString);
        
        var result = UsernameValidator.Validate(displayname);

        if (!result.Ok) return new ValidationResult(result.ErrorMessage);
        
        return ValidationResult.Success;
    }

    /// <inheritdoc/>
    public void Apply(OpenApiSchema schema)
    {
        if (ShouldValidate) schema.Pattern = DisplaynameRegex;
        
        schema.Example = new OpenApiString(ExampleDisplayname);
    }

    /// <inheritdoc/>
    public void Apply(OpenApiParameter parameter) => Apply(parameter.Schema);
}