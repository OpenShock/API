// ReSharper disable InconsistentNaming

using Microsoft.Extensions.Options;
using OpenShock.Common.Geo;
using System.ComponentModel.DataAnnotations;

namespace OpenShock.LiveControlGateway.Options;

/// <summary>
/// Config for the LCG
/// </summary>
public sealed class LcgOptions
{
    /// <summary>
    /// IConfiguration section path
    /// </summary>
    public const string SectionName = "OpenShock:LCG";
    
    /// <summary>
    /// FQDN of the LCG
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string Fqdn { get; set; }

    /// <summary>
    /// A valid country code by ISO 3166-1 alpha-2 https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
    /// </summary>
    [Alpha2CountryCode]
    public required string CountryCode { get; set; }
}

/// <summary>
/// Options validator for <see cref="LcgOptions"/>
/// </summary>
[OptionsValidator]
public partial class LcgOptionsValidator : IValidateOptions<LcgOptions>
{
}