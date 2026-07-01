using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Requests;

public class EditTokenRequestV2
{
    [StringLength(HardLimits.ApiKeyNameMaxLength, MinimumLength = 1, ErrorMessage = "API token length must be between {1} and {2}")]
    public required string Name { get; set; }

    [MaxLength(HardLimits.ApiKeyMaxPermissions, ErrorMessage = "API token permissions must be between {1} and {2}")]
    public required IReadOnlyList<PermissionType> Permissions { get; set; }

    /// <summary>
    /// Per-token shocker control configuration. When omitted, the permissive default is used.
    /// </summary>
    public required ShockerControlSettings ShockerControl { get; set; }
}
