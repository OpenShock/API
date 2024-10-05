using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Tokens;

public sealed partial class TokensController
{
    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <param name="tokenService"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    [TokenOnly]
    [ProducesSuccess<TokenResponse>]
    public TokenResponse GetSelfToken([FromServices] ITokenReferenceService<ApiToken> tokenService)
    {
        var x = tokenService.Token;
        if (x?.Token == null) throw new Exception("This should not be reachable due to the [TokenOnly] attribute");
        return new TokenResponse
        {
            CreatedOn = x.CreatedOn,
            ValidUntil = x.ValidUntil,
            LastUsed = x.LastUsed,
            Permissions = x.Permissions,
            Name = x.Name,
            Id = x.Id
        };
    }
}