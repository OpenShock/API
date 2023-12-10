using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;

namespace OpenShock.API.Controller.Account;

partial class AccountController
{
    /// <summary>
    /// Signs up a user
    /// </summary>
    /// <param name="data"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="400">Username or email already exists</response>
    [HttpPost("signup", Name = "Signup")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<BaseResponse<object>> Signup([FromBody] Signup data)
    {
        var newGuid = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = newGuid,
            Name = data.Username,
            Email = data.Email.ToLowerInvariant(),
            Password = SecurePasswordHasher.Hash(data.Password),
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