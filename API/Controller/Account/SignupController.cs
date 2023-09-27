using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon;

namespace OpenShock.API.Controller.Account;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}/account/signup")]
public class SignupController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    
    public SignupController(OpenShockContext db)
    {
        _db = db;
    }
    
    [HttpPost]
    public async Task<BaseResponse<object>> Signup(Signup data)
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