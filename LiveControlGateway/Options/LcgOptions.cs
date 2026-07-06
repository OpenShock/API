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
    /// FQDN (public host) of the LCG that clients connect to
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string Fqdn { get; set; }

    /// <summary>
    /// Public port clients (firmware &amp; browsers) connect to. Defaults to 443.
    /// </summary>
    public ushort PublicPort { get; set; } = 443;

    /// <summary>
    /// Public path prefix the gateway is served under (e.g. "/gateway"). Empty = root.
    /// </summary>
    public string PublicPath { get; set; } = string.Empty;

    /// <summary>
    /// A valid country code by ISO 3166-1 alpha-2 https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
    /// </summary>
    [Alpha2CountryCode]
    public required string CountryCode { get; set; }

    /// <summary>
    /// Normalized public path prefix: a single leading slash and no trailing slash, or empty for root.
    /// ("gateway", "/gateway", "/gateway/" all become "/gateway"; "" / "/" become "").
    /// </summary>
    public string NormalizedPublicPath
    {
        get
        {
            var trimmed = PublicPath.Trim().Trim('/');
            return trimmed.Length == 0 ? string.Empty : "/" + trimmed;
        }
    }

    /// <summary>
    /// The gateway's public node id: its full public base as <c>host[:port][/path]</c>. The port and
    /// path are only appended when they differ from the defaults (443 / root), so a gateway on the
    /// default port and root path keeps the bare-host id it had historically.
    /// </summary>
    public string GetPublicNodeId()
    {
        var id = Fqdn;
        if (PublicPort != 443) id += ":" + PublicPort;
        id += NormalizedPublicPath;
        return id;
    }
}

/// <summary>
/// Options validator for <see cref="LcgOptions"/>
/// </summary>
[OptionsValidator]
public partial class LcgOptionsValidator : IValidateOptions<LcgOptions>
{
}