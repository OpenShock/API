namespace OpenShock.API.Controller.Admin.DTOs;

/// <summary>
/// Outcome of adding domains to the email provider blacklist.
/// </summary>
public sealed class AddEmailProviderBlacklistResultDto
{
    /// <summary>Distinct domains in the request, after normalising and dropping blank entries.</summary>
    public required int Submitted { get; init; }

    /// <summary>Domains that were newly added.</summary>
    public required int Added { get; init; }

    /// <summary>Domains that were already on the blacklist.</summary>
    public int AlreadyListed => Submitted - Added;
}
