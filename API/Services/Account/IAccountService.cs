using OneOf;
using OneOf.Types;

namespace OpenShock.API.Services.Account;

/// <summary>
/// Handles account related operations like signup, login, password reset, email verification, etc.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Creates an account 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username, string password);
    
    /// <summary>
    /// When a user uses the signup form, this also handles email verification mail
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<OneOf<Success, AccountWithEmailOrUsernameExists>> Signup(string email, string username, string password);
    
}

public struct AccountWithEmailOrUsernameExists;