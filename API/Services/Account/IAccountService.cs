using OneOf;
using OneOf.Types;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Validation;

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
    /// <returns></returns>
    public Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username, string password);
    
    public Task<OneOf<Success, CannotDeactivatePrivilegedAccount, AccountDeactivationAlreadyInProgress, NotFound>> DeactivateAccount(Guid executingUserId, Guid userId, bool deleteLater = true);
    
    public Task<OneOf<Success, NotFound>> ReactivateAccount(Guid userId);
    
    public Task<OneOf<Success, CannotDeletePrivilegedAccount, NotFound>> DeleteAccount(Guid executingUserId, Guid userId);

    /// <summary>
    /// When a user uses the signup form, this also handles email verification mail
    /// </summary>
    /// <param name="email"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> Signup(string email, string username, string password);

    /// <summary>
    /// Login a user into his user session
    /// </summary>
    /// <param name="usernameOrEmail"></param>
    /// <param name="password"></param>
    /// <param name="loginContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success<string>, NotFound>> Login(string usernameOrEmail, string password, LoginContext loginContext, CancellationToken cancellationToken = default);
    
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
    
    /// <summary>
    /// Check the availability of a username
    /// </summary>
    /// <param name="username"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success, UsernameTaken, UsernameError>> CheckUsernameAvailability(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change the username of a user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="username"></param>
    /// <param name="ignoreLimit">Ignore the username change limit, set this to true when an admin is changing the username</param>
    /// <returns><see cref="Error{UsernameCheckResult}"/> only returns when the result is != Available</returns>
    public Task<OneOf<Success, Error<OneOf<UsernameTaken, UsernameError, RecentlyChanged>>, NotFound>> ChangeUsername(Guid userId, string username, bool ignoreLimit = false);
    
    /// <summary>
    /// Change the password of a user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    public Task<OneOf<Success, NotFound>> ChangePassword(Guid userId, string newPassword);
}

public readonly struct AccountWithEmailOrUsernameExists;
public readonly struct CannotDeactivatePrivilegedAccount;
public readonly struct AccountDeactivationAlreadyInProgress;
public readonly struct CannotDeletePrivilegedAccount;
public readonly struct TooManyPasswordResets;
public readonly struct SecretInvalid;

public readonly struct UsernameTaken;

public readonly struct RecentlyChanged;