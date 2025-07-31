using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;
using OpenShock.Common.Validation;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Services.Account;

/// <summary>
/// Default implementation of IAccountService
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly OpenShockContext _db;
    private readonly IEmailService _emailService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AccountService> _logger;
    private readonly FrontendOptions _frontendConfig;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="emailService"></param>
    /// <param name="sessionService"></param>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    public AccountService(OpenShockContext db, IEmailService emailService,
        ISessionService sessionService, ILogger<AccountService> logger, IOptions<FrontendOptions> options)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _frontendConfig = options.Value;
        _sessionService = sessionService;
    }

    /// <inheritdoc />
    public Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateUnverifiedAccountLegacyAsync(string email, string username,
        string password)
    {
        return CreateAccount(email, username, password, true);
    }

    public async Task<OneOf<Success, CannotDeactivatePrivilegedAccount, AccountDeactivationAlreadyInProgress, Unauthorized, NotFound>> DeactivateAccountAsync(Guid executingUserId, Guid userId, bool deleteLater)
    {
        if (executingUserId != userId)
        {
            var isPrivileged = await _db.Users
                            .Where(u => u.Id == executingUserId)
                            .SelectMany(u => u.Roles)
                            .AnyAsync(r =>
                                r == RoleType.Staff ||
                                r == RoleType.Admin ||
                                r == RoleType.System);
            if (!isPrivileged)
            {
                return new Unauthorized();
            }
        }

        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return new NotFound();

        if (user.Roles.Any(r => r is RoleType.Admin or RoleType.System))
        {
            return new CannotDeactivatePrivilegedAccount();
        }

        if (user.UserDeactivation is not null)
        {
            return new AccountDeactivationAlreadyInProgress();
        }

        user.UserDeactivation = new UserDeactivation
        {
            DeactivatedUserId = userId,
            DeactivatedByUserId = executingUserId,
            DeleteLater = deleteLater,
        };

        await _db.SaveChangesAsync();

        // Remove all login sessions
        await _sessionService.DeleteSessionsByUserIdAsync(userId);

        return new Success();
    }

    public async Task<OneOf<Success, Unauthorized, NotFound>> ReactivateAccountAsync(Guid executingUserId, Guid userId)
    {
        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(u => u.Id == userId && u.UserDeactivation != null);
        if (user is null) return new NotFound();

        var deactivation = user.UserDeactivation!;
        bool isSelfReactivation =
            executingUserId == userId &&
            deactivation.DeactivatedByUserId == deactivation.DeactivatedUserId;

        if (!isSelfReactivation)
        {
            var isPrivileged = await _db.Users
                            .Where(u => u.Id == executingUserId)
                            .SelectMany(u => u.Roles)
                            .AnyAsync(r =>
                                r == RoleType.Staff ||
                                r == RoleType.Admin ||
                                r == RoleType.System);
            if (!isPrivileged)
            {
                return new Unauthorized();
            }
        }

        _db.Remove(deactivation);
        await _db.SaveChangesAsync();

        return new Success();
    }

    public async Task<OneOf<Success, CannotDeletePrivilegedAccount, Unauthorized, NotFound>> DeleteAccountAsync(Guid executingUserId, Guid userId)
    {
        var isPrivileged = await _db.Users
                        .Where(u => u.Id == executingUserId)
                        .SelectMany(u => u.Roles)
                        .AnyAsync(r =>
                            r == RoleType.Staff ||
                            r == RoleType.Admin ||
                            r == RoleType.System);
        if (!isPrivileged)
        {
            return new Unauthorized();
        }

        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return new NotFound();

        if (user.Roles.Any(r => r is RoleType.Admin or RoleType.System))
        {
            return new CannotDeletePrivilegedAccount();
        }

        // TODO: Do more checks?

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return new Success();
    }

    private async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccount(string email,
        string username,
        string password, bool emailActivated)
    {
        if (await _db.Users.AnyAsync(x => x.Email == email.ToLowerInvariant() || x.Name == username))
            return new AccountWithEmailOrUsernameExists();

        var newGuid = Guid.CreateVersion7();
        var user = new User
        {
            Id = newGuid,
            Name = username,
            Email = email.ToLowerInvariant(),
            PasswordHash = HashingUtils.HashPassword(password)
        };
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return new Success<User>(user);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccountWithVerificationFlowAsync(string email, string username,
        string password)
    {
        var accountCreate = await CreateAccount(email, username, password, false);
        if (accountCreate.IsT1) return accountCreate;

        var user = accountCreate.AsT0.Value;

        var secret = CryptoUtils.RandomString(AuthConstants.GeneratedTokenLength);

        user.UserActivationRequest = new UserActivationRequest
        {
            UserId = user.Id,
            SecretHash = HashingUtils.HashToken(secret)
        };

        await _db.SaveChangesAsync();

        await _emailService.VerifyEmail(new Contact(email, username),
            new Uri(_frontendConfig.BaseUrl, $"/#/account/activate/{user.Id}/{secret}"));
        return new Success<User>(user);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success<string>, AccountDeactivated, NotFound>> CreateUserLoginSessionAsync(string usernameOrEmail, string password,
        LoginContext loginContext, CancellationToken cancellationToken = default)
    {
        var lowercaseUsernameOrEmail = usernameOrEmail.ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.UserDeactivation)
            .FirstOrDefaultAsync(x => x.Email == lowercaseUsernameOrEmail || x.Name == lowercaseUsernameOrEmail, cancellationToken);
        if (user is null)
        {
            // TODO: Set appropriate time to match password hashing time, preventing timing attacks
            await Task.Delay(100, cancellationToken);
            return new NotFound();
        }
        if (user.UserDeactivation is not null)
        {
            return new AccountDeactivated();
        }

        if (!await CheckPassword(password, user)) return new NotFound();

        var createdSession = await _sessionService.CreateSessionAsync(user.Id, loginContext.UserAgent, loginContext.Ip);

        return new Success<string>(createdSession.Token);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, SecretInvalid>> CheckPasswordResetExistsAsync(Guid passwordResetId, string secret,
        CancellationToken cancellationToken = default)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var reset = await _db.UserPasswordResets.FirstOrDefaultAsync(x =>
                x.Id == passwordResetId && x.UsedAt == null && x.CreatedAt >= validSince,
            cancellationToken: cancellationToken);

        if (reset is null) return new NotFound();

        var result = HashingUtils.VerifyToken(secret, reset.SecretHash);
        if (!result.Verified) return new SecretInvalid();
        
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, TooManyPasswordResets, AccountDeactivated, NotFound>> CreatePasswordResetFlowAsync(string email)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var lowerCaseEmail = email.ToLowerInvariant();
        var user = await _db.Users
            .Where(x => x.Email == lowerCaseEmail)
            .Include(x => x.UserDeactivation)
            .Select(x => new
            {
                User = x,
                PasswordResetCount = x.PasswordResets.Count(y => y.UsedAt == null && y.CreatedAt >= validSince)
            })
            .FirstOrDefaultAsync();
        if (user is null) return new NotFound();
        if (user.User.UserDeactivation is not null) return new AccountDeactivated();
        if (user.PasswordResetCount >= 3) return new TooManyPasswordResets();

        var secret = CryptoUtils.RandomString(AuthConstants.GeneratedTokenLength);
        var secretHash = HashingUtils.HashToken(secret);
        var passwordReset = new UserPasswordReset
        {
            Id = Guid.CreateVersion7(),
            UserId = user.User.Id,
            SecretHash = secretHash
        };
        _db.UserPasswordResets.Add(passwordReset);
        await _db.SaveChangesAsync();

        await _emailService.PasswordReset(new Contact(user.User.Email, user.User.Name),
            new Uri(_frontendConfig.BaseUrl, $"/#/account/password/recover/{passwordReset.Id}/{secret}"));

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, AccountDeactivated, SecretInvalid>> CompletePasswordResetFlowAsync(Guid passwordResetId,
        string secret, string newPassword)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;

        var reset = await _db.UserPasswordResets
            .Include(x => x.User)
            .Include(x => x.User.UserDeactivation)
            .FirstOrDefaultAsync(x => x.Id == passwordResetId && x.UsedAt == null && x.CreatedAt >= validSince);
        if (reset is null) return new NotFound();
        if (reset.User.UserDeactivation is not null) return new AccountDeactivated();

        var result = HashingUtils.VerifyToken(secret, reset.SecretHash);
        if (!result.Verified) return new SecretInvalid();

        reset.UsedAt = DateTime.UtcNow;
        reset.User.PasswordHash = HashingUtils.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, UsernameTaken, UsernameError>> CheckUsernameAvailabilityAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var validationResult = UsernameValidator.Validate(username);
        if (validationResult.IsT1)
            return validationResult.AsT1;

        var isTaken = await _db.Users.AnyAsync(x => x.Name == username, cancellationToken: cancellationToken);
        if (isTaken) return new UsernameTaken();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, UsernameTaken, UsernameError, RecentlyChanged, AccountDeactivated, NotFound>> ChangeUsernameAsync(Guid userId, string username, bool ignoreLimit = false, CancellationToken cancellationToken = default)
    {
        if (!ignoreLimit)
        {
            var cooldownSubtracted = DateTime.UtcNow.Subtract(Duration.NameChangeCooldown);
            if (await _db.UserNameChanges.Where(x => x.UserId == userId && x.CreatedAt >= cooldownSubtracted).AnyAsync(cancellationToken))
            {
                return new RecentlyChanged();
            }
        }

        var availability = await CheckUsernameAvailabilityAsync(username, cancellationToken);
        if (availability.IsT1) return availability.AsT1;
        if (availability.IsT2) return availability.AsT2;

        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return new NotFound();
        if (user.UserDeactivation is not null) return new AccountDeactivated();
        if (user.Name == username) return new Success(); // Unchanged

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var oldName = user.Name;

        user.Name = username;
        await _db.SaveChangesAsync(cancellationToken);

        _db.UserNameChanges.Add(new UserNameChange
        {
            UserId = userId,
            OldName = oldName
        });

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new Success();
    }


    /// <inheritdoc />
    public async Task<OneOf<Success, AccountDeactivated, NotFound>> ChangePasswordAsync(Guid userId, string newPassword)
    {
        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return new NotFound();
        if (user.UserDeactivation is not null) return new AccountDeactivated();

        user.PasswordHash = HashingUtils.HashPassword(newPassword);

        await _db.SaveChangesAsync();

        return new Success();
    }


    private async Task<bool> CheckPassword(string password, User user)
    {
        var result = HashingUtils.VerifyPassword(password, user.PasswordHash);

        if (!result.Verified)
        {
            _logger.LogInformation("Failed to verify password for user ID: [{Id}]", user.Id);
            return false;
        }

        if (result.NeedsRehash)
        {
            _logger.LogInformation("Rehashing password for user ID: [{Id}]", user.Id);
            user.PasswordHash = HashingUtils.HashPassword(password);
            await _db.SaveChangesAsync();
        }

        return true;
    }
}