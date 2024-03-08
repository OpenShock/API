using OneOf;
using OneOf.Types;

namespace OpenShock.API.Services.Account;

/// <summary>
/// Handles account related operations like signup, login, password reset, email verification, etc.
/// </summary>
public interface IAccountService
{
    public TimeSpan SessionLifetime { get; }
    
    /// <summary>
    /// Creates an account 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<OneOf<Success, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username, string password);

    /// <summary>
    /// When a user uses the signup form, this also handles email verification mail
    /// </summary>
    /// <param name="email"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<OneOf<Success, AccountWithEmailOrUsernameExists>> Signup(string email, string username, string password);

    /// <summary>
    /// Login a user into his user session
    /// </summary>
    /// <param name="emailOrUsername"></param>
    /// <param name="password"></param>
    /// <param name="loginContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success<string>, NotFound>> Login(string emailOrUsername, string password, LoginContext loginContext, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a password reset request exists and the secret is valid
    /// </summary>
    /// <param name="passwordResetId"></param>
    /// <param name="secret"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success, NotFound, SecretInvalid>> PasswordResetExists(Guid passwordResetId, string secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new password reset request and send the email if successful
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<OneOf<Success, TooManyPasswordResets, NotFound>> CreatePasswordReset(string email);
    
    /// <summary>
    /// Completes a password reset process, sets a new password
    /// </summary>
    /// <param name="passwordResetId"></param>
    /// <param name="secret"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    public Task<OneOf<Success, NotFound, SecretInvalid>> PasswordResetComplete(Guid passwordResetId, string secret, string newPassword);
}

public struct AccountWithEmailOrUsernameExists;
public struct TooManyPasswordResets;
public struct SecretInvalid;