using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;
using Asp.Versioning;
using OpenShock.ServicesCommon.Services.Turnstile;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="400">Username or email already exists</response>
    [HttpPost("signup")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [MapToApiVersion("2")]
    public async Task<BaseResponse<object>> SignUpV2([FromBody] SignUpV2 body, [FromServices] ICloudflareTurnstileService turnstileService, CancellationToken cancellationToken)
    {
        var turnStile = await turnstileService.VerifyUserResponseToken(body.TurnstileResponse, HttpContext.Connection.RemoteIpAddress, cancellationToken);
                if (!turnStile.IsT0) return EBaseResponse<object>("Invalid turnstile response", HttpStatusCode.Forbidden);
        
        var newGuid = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = newGuid,
            Name = body.Username,
            Email = body.Email.ToLowerInvariant(),
            Password = SecurePasswordHasher.Hash(body.Password),
            EmailActived = true
        });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e)
        {
            if (e.InnerException is PostgresException exception)
            {
                switch (exception.SqlState)
                {
                    case PostgresErrorCodes.UniqueViolation:
                        return EBaseResponse<object>(
                            "Account with same username or email already exists. Please choose a different username or reset your password.");
                    default:
                        throw;
                }
            }
        }
        
        return new BaseResponse<object>("Successfully created account");
    }
}