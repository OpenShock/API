using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.Geo;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class Alpha2CountryCodeAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string asString)
            return new ValidationResult("Input type must be string");

        if (asString is not [>= 'A' and <= 'Z', >= 'A' and <= 'Z'])
            return new ValidationResult("Input string must be exactly 2 uppercase characters");

        if (!Alpha2CountryCode.TryParse(asString, out var countryCode))
            return new ValidationResult($"Failed to create {nameof(Alpha2CountryCode)}");
        
        if (!countryCode.IsUnknown() && !CountryInfo.CodeDictionary.ContainsKey(countryCode))
            return new ValidationResult("Country does not exist in mapping");

        return ValidationResult.Success;
    }
}