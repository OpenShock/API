using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;
using Asp.Versioning;

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
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> SignUp([FromBody] SignUp body)
    {
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
            await _db.SaveChangesAsync();
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