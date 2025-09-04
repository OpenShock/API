using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.OAuthConnection;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// List OAuth connections linked to the current user.
    /// </summary>
    /// <returns>Array of connections with provider key, external id, display name and link time.</returns>
    /// <response code="200">Returns the list of connections.</response>
    [HttpGet("connections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<OAuthConnectionResponse[]> ListOAuthConnections([FromServices] IOAuthConnectionService connectionService)
    {
        var connections = await connectionService.GetConnectionsAsync(CurrentUser.Id);

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