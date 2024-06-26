﻿// ReSharper disable InconsistentNaming

using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Config;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Config for the LCG
/// </summary>
public class LCGConfig : BaseConfig
{
    /// <summary>
    /// LCG specific config instance
    /// </summary>
    [Required] public required LCGPartConfig Lcg { get; init; }
    
    /// <summary>
    /// LCG specific config
    /// </summary>
    public sealed class LCGPartConfig
    {
        /// <summary>
        /// FQDN of the LCG
        /// </summary>
        [Required(AllowEmptyStrings = false)] public required string Fqdn { get; set; }
    
        /// <summary>
        /// A valid country code by ISO 3166-1 alpha-2 https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// </summary>
        [Alpha2CountryCode]
        public required string CountryCode { get; set; }
    }
}