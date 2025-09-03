using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// List OAuth connections
    /// </summary>
    [HttpGet("connections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<OAuthConnectionResponse[]> ListOAuthConnections()
    {
        var connections = await _accountService.GetOAuthConnectionsAsync(CurrentUser.Id);

        return connections
            .Select(c => new OAuthConnectionResponse
            {
                ProviderKey = c.ProviderKey,
                ExternalId = c.ExternalId,
                DisplayName = c.DisplayName,
                LinkedAt = c.CreatedAt
            })
            .ToArray();
    }
}