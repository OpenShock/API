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
    public Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccountWithoutVerificationFlowLegacyAsync(string email, string username, string password);
    
    /// <summary>
    /// When a user uses the signup form, this also handles email verification mail
    /// </summary>
    /// <param name="email"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccountWithVerificationFlowAsync(string email, string username, string password);

    public Task<OneOf<Success, CannotDeactivatePrivilegedAccount, AccountDeactivationAlreadyInProgress, Unauthorized, NotFound>> DeactivateAccountAsync(Guid executingUserId, Guid userId, bool deleteLater = true);
    
    public Task<OneOf<Success, Unauthorized, NotFound>> ReactivateAccountAsync(Guid executingUserId, Guid userId);

    public Task<OneOf<Success, CannotDeletePrivilegedAccount, Unauthorized, NotFound>> DeleteAccountAsync(Guid executingUserId, Guid userId);

    /// <summary>
    /// Login a user into his user session
    /// </summary>
    /// <param name="usernameOrEmail"></param>
    /// <param name="password"></param>
    /// <param name="loginContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success<string>, AccountDeactivated, NotFound>> CreateUserLoginSessionAsync(string usernameOrEmail, string password, LoginContext loginContext, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a password reset request exists and the secret is valid
    /// </summary>
    /// <param name="passwordResetId"></param>
    /// <param name="secret"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success, NotFound, SecretInvalid>> CheckPasswordResetExistsAsync(Guid passwordResetId, string secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new password reset request and send the email if successful
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<OneOf<Success, TooManyPasswordResets, AccountDeactivated, NotFound>> CreatePasswordResetFlowAsync(string email);
    
    /// <summary>
    /// Completes a password reset process, sets a new password
    /// </summary>
    /// <param name="passwordResetId"></param>
    /// <param name="secret"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    public Task<OneOf<Success, NotFound, AccountDeactivated, SecretInvalid>> CompletePasswordResetFlowAsync(Guid passwordResetId, string secret, string newPassword);
    
    /// <summary>
    /// Check the availability of a username
    /// </summary>
    /// <param name="username"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OneOf<Success, UsernameTaken, UsernameError>> CheckUsernameAvailabilityAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change the username of a user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="username"></param>
    /// <param name="ignoreLimit">Ignore the username change limit, set this to true when an admin is changing the username</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Error{UsernameCheckResult}"/> only returns when the result is != Available</returns>
    public Task<OneOf<Success, UsernameTaken, UsernameError, RecentlyChanged, AccountDeactivated, NotFound>> ChangeUsernameAsync(Guid userId, string username, bool ignoreLimit = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Change the password of a user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="newPassword"></param>
    /// <returns></returns>
    public Task<OneOf<Success, AccountDeactivated, NotFound>> ChangePasswordAsync(Guid userId, string newPassword);
}

public readonly record struct AccountDeactivated;
public readonly struct AccountWithEmailOrUsernameExists;
public readonly struct CannotDeactivatePrivilegedAccount;
public readonly struct AccountDeactivationAlreadyInProgress;
public readonly struct CannotDeletePrivilegedAccount;
public readonly struct TooManyPasswordResets;
public readonly struct SecretInvalid;
public readonly struct Unauthorized;

public readonly struct UsernameTaken;

public readonly struct RecentlyChanged;