namespace OpenShock.API.Controller.Admin.DTOs;

public sealed class AddEmailProviderBlacklistDto
{
    /// <summary>
    /// Domains to blacklist. Full email addresses are accepted too and contribute their domain; blank entries are
    /// ignored. Every other entry must be a valid domain, or nothing is added.
    /// </summary>
    public required string[] Domains { get; init; }
}
