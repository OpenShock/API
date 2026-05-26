using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        ISessionService sessionService, ILogger<AccountService> logger, FrontendOptions options)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _frontendConfig = options;
        _sessionService = sessionService;
    }

    private async Task<bool> IsUserNameBlacklisted(string username)
    {
        await foreach (var entry in _db.UserNameBlacklists.AsNoTracking().AsAsyncEnumerable())
        {
            if (entry.IsMatch(username)) return true;
        }

        return false;
    }

    private async Task<bool> IsEmailProviderBlacklisted(string email)
    {
        if (!MailAddress.TryCreate(email, out var address)) return false;
        var domain = address.Host.ToLowerInvariant();
        return await _db.EmailProviderBlacklists.AnyAsync(e => e.Domain == domain);
    }

    private async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username, string password, bool verifyOnCreation)
    {
        email = email.ToLowerInvariant();

        if (await IsUserNameBlacklisted(username) || await IsEmailProviderBlacklisted(email))
            return new AccountWithEmailOrUsernameExists();
        
        if (await _db.Users.AnyAsync(x => x.Email == email || x.Name == username))
            return new AccountWithEmailOrUsernameExists();

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Name = username,
            Email = email,
            PasswordHash = HashingUtils.HashPassword(password)
        };
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        // Use date created by the database to keep timing consistent
        if (verifyOnCreation)
        {
            await _db.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(spc => spc.SetProperty(u => u.ActivatedAt, u => u.CreatedAt));
        }

        return new Success<User>(user);
    }
    
    /// <inheritdoc />
    public async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccountWithoutActivationFlowLegacyAsync(string email, string username, string password)
    {
        return await CreateAccount(email, username, password, true);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccountWithActivationFlowAsync(string email, string username, string password)
    {
        var accountCreate = await CreateAccount(email, username, password, false);
        if (accountCreate.IsT1) return accountCreate;

        var user = accountCreate.AsT0.Value;

        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);

        user.UserActivationRequest = new UserActivationRequest
        {
            UserId = user.Id,
            TokenHash = HashingUtils.HashToken(token)
        };

        await _db.SaveChangesAsync();

        await _emailService.ActivateAccount(new Contact(email, username),
            new Uri(_frontendConfig.BaseUrl, $"/activate?token={token}"));
        return new Success<User>(user);
    }

    public Task<bool> IsEmailRegisteredAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.ToLowerInvariant();
        return _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateOAuthOnlyAccountAsync(
        string email,
        string username,
        string provider,
        string providerAccountId,
        string? providerAccountName,
        bool isEmailTrusted)
    {
        email = email.ToLowerInvariant();
        provider = provider.ToLowerInvariant();

        // Reuse your existing guards
        if (await IsUserNameBlacklisted(username) || await IsEmailProviderBlacklisted(email))
            return new AccountWithEmailOrUsernameExists();

        // Fast uniqueness check (optimistic; race handled by unique constraints below)
        var exists = await _db.Users.AnyAsync(u => u.Email == email || u.Name == username);
        if (exists) return new AccountWithEmailOrUsernameExists();

        await using var tx = await _db.Database.BeginTransactionAsync();

        string? activationToken = null;

        try
        {
            var creationTime = DateTime.UtcNow;
            
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                Name = username,
                Email = email,
                PasswordHash = null,
                CreatedAt = creationTime,
                ActivatedAt = isEmailTrusted ? creationTime : null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // If email isn't trusted, create an activation request (email verification)
            if (!isEmailTrusted)
            {
                activationToken = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);

                user.UserActivationRequest = new UserActivationRequest
                {
                    UserId = user.Id,
                    TokenHash = HashingUtils.HashToken(activationToken),
                    CreatedAt = creationTime
                };

                await _db.SaveChangesAsync();
            }

            // Link external identity
            _db.UserOAuthConnections.Add(new UserOAuthConnection
            {
                UserId = user.Id,
                ProviderKey = provider,
                ExternalId = providerAccountId,
                DisplayName = providerAccountName,
                CreatedAt = creationTime,
            });

            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // Send verification email only after a successful commit
            if (!isEmailTrusted && activationToken is not null)
            {
                await _emailService.ActivateAccount(
                    new Contact(email, username),
                    new Uri(_frontendConfig.BaseUrl, $"/activate?token={activationToken}")
                );
            }

            return new Success<User>(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            await tx.RollbackAsync();
            return new AccountWithEmailOrUsernameExists();
        }
    }


    public async Task<bool> TryActivateAccountAsync(string secret, CancellationToken cancellationToken = default)
    {
        var hash = HashingUtils.HashToken(secret);

        var user = await _db.Users
            .Include(u => u.UserActivationRequest)
            .FirstOrDefaultAsync(x => x.UserDeactivation == null && x.UserActivationRequest != null && x.UserActivationRequest.TokenHash == hash, cancellationToken);
        if (user?.UserActivationRequest is null) return false;

        user.ActivatedAt = DateTime.UtcNow;

        _db.UserActivationRequests.Remove(user.UserActivationRequest);

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc/>
    public async Task<OneOf<User, NotFound, AccountDeactivated, AccountNotActivated, AccountIsOAuthOnly>> GetAccountByCredentialsAsync(string usernameOrEmail, string password,  CancellationToken cancellationToken)
    {
        var lowercaseUsernameOrEmail = usernameOrEmail.ToLowerInvariant();
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserDeactivation)
            .FirstOrDefaultAsync(x => x.Email == lowercaseUsernameOrEmail || x.Name == lowercaseUsernameOrEmail, cancellationToken);
        if (user is null)
        {
            await HashingUtils.VerifyPasswordFake();
            return new NotFound();
        }
        
        if (user.PasswordHash is null)
        {
            await HashingUtils.VerifyPasswordFake();
            return new AccountIsOAuthOnly();
        }

        if (!await CheckPassword(password, user))
        {
            return new NotFound();
        }
        
        if (user.UserDeactivation is not null)
        {
            return new AccountDeactivated();
        }

        if (user.ActivatedAt is null)
        {
            return new AccountNotActivated();
        }

        return user;
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, SecretInvalid>> CheckPasswordResetExistsAsync(Guid passwordResetId, string secret,
        CancellationToken cancellationToken = default)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var reset = await _db.UserPasswordResets.FirstOrDefaultAsync(x =>
                x.Id == passwordResetId && x.UsedAt == null && x.CreatedAt >= validSince
                && x.SecurityStampAtCreate == x.User.SecurityStamp,
            cancellationToken: cancellationToken);

        if (reset is null) return new NotFound();

        var result = HashingUtils.VerifyToken(secret, reset.TokenHash);
        if (!result.Verified) return new SecretInvalid();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, TooManyPasswordResets, AccountNotActivated, AccountDeactivated, NotFound>> CreatePasswordResetFlowAsync(string email)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var lowerCaseEmail = email.ToLowerInvariant();
        var user = await _db.Users
            .Where(x => x.Email == lowerCaseEmail)
            .Select(x => new
            {
                User = x,
                IsDeactivated = x.UserDeactivation != null,
                PasswordResetCount = x.PasswordResets.Count(y => y.UsedAt == null && y.CreatedAt >= validSince)
            })
            .FirstOrDefaultAsync();
        if (user is null) return new NotFound();
        if (user.User.ActivatedAt is null) return new AccountNotActivated();
        if (user.IsDeactivated) return new AccountDeactivated();
        if (user.PasswordResetCount >= 3) return new TooManyPasswordResets();

        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);
        var passwordReset = new UserPasswordReset
        {
            Id = Guid.CreateVersion7(),
            UserId = user.User.Id,
            TokenHash = HashingUtils.HashToken(token),
            SecurityStampAtCreate = user.User.SecurityStamp
        };
        _db.UserPasswordResets.Add(passwordReset);
        await _db.SaveChangesAsync();

        await _emailService.PasswordReset(new Contact(user.User.Email, user.User.Name),
            new Uri(_frontendConfig.BaseUrl, $"/#/account/password/recover/{passwordReset.Id}/{token}"));

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, AccountNotActivated, AccountDeactivated, SecretInvalid>> CompletePasswordResetFlowAsync(Guid passwordResetId,
        string secret, string newPassword)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;

        var reset = await _db.UserPasswordResets
            .Select(x => new
            {
                Reset = x,
                UserActivatedAt = x.User.ActivatedAt,
                IsDeactivated = x.User.UserDeactivation != null,
                UserSecurityStamp = x.User.SecurityStamp
            })
            .FirstOrDefaultAsync(x => x.Reset.Id == passwordResetId && x.Reset.UsedAt == null && x.Reset.CreatedAt >= validSince
                && x.Reset.SecurityStampAtCreate == x.UserSecurityStamp);
        if (reset is null) return new NotFound();
        if (reset.UserActivatedAt is null) return new AccountNotActivated();
        if (reset.IsDeactivated) return new AccountDeactivated();

        var result = HashingUtils.VerifyToken(secret, reset.Reset.TokenHash);
        if (!result.Verified) return new SecretInvalid();

        // Race-safe consume + apply: only updates if SecurityStamp still matches the snapshot.
        // If a sibling reset (or a separate password/email change) completed since the read above,
        // the stamp has rotated and the predicate matches zero rows.
        var newPasswordHash = HashingUtils.HashPassword(newPassword);
        var newStamp = Guid.CreateVersion7();
        var userRows = await _db.Users
            .Where(u => u.Id == reset.Reset.UserId && u.SecurityStamp == reset.Reset.SecurityStampAtCreate)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, newPasswordHash)
                .SetProperty(u => u.SecurityStamp, newStamp));
        if (userRows == 0) return new NotFound();

        var now = DateTime.UtcNow;
        await _db.UserPasswordResets
            .Where(r => r.Id == reset.Reset.Id && r.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UsedAt, now));

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, UsernameTaken, UsernameError>> CheckUsernameAvailabilityAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var validationResult = UsernameValidator.Validate(username);
        if (validationResult.IsT1)
            return validationResult.AsT1;

        if (await IsUserNameBlacklisted(username))
            return new UsernameTaken(); // Don't inform the user about when the blacklist is hit

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
    public async Task<OneOf<Success, AccountNotActivated, AccountDeactivated, NotFound>> ChangePasswordAsync(Guid userId, string newPassword)
    {
        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return new NotFound();
        if (user.ActivatedAt is null) return new AccountNotActivated();
        if (user.UserDeactivation is not null) return new AccountDeactivated();

        user.PasswordHash = HashingUtils.HashPassword(newPassword);
        user.SecurityStamp = Guid.CreateVersion7(); // Any outstanding reset/email-change row for this user has a stale SecurityStampAtCreate after this; predicate handles invalidation.

        await _db.SaveChangesAsync();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, EmailAlreadyInUse, EmailUnchanged, TooManyEmailChanges, AccountNotActivated, AccountDeactivated, NotFound>> CreateEmailChangeFlowAsync(Guid userId, string newEmail)
    {
        var validSince = DateTime.UtcNow - Duration.EmailChangeRequestLifetime;

        var data = await _db.Users
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                User = x,
                IsDeactivated = x.UserDeactivation != null,
                PendingCount = x.EmailChanges.Count(y => y.UsedAt == null && y.CreatedAt >= validSince)
            })
            .FirstOrDefaultAsync();
        if (data is null) return new NotFound();
        if (data.User.ActivatedAt is null) return new AccountNotActivated();
        if (data.IsDeactivated) return new AccountDeactivated();
        if (string.Equals(data.User.Email, newEmail, StringComparison.OrdinalIgnoreCase)) return new EmailUnchanged();
        if (data.PendingCount >= 3) return new TooManyEmailChanges();

        if (await IsEmailProviderBlacklisted(newEmail))
            return new EmailAlreadyInUse(); // Don't reveal blacklist hits

        var lowerCaseEmail = newEmail.ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == lowerCaseEmail))
            return new EmailAlreadyInUse();

        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);
        var emailChange = new UserEmailChange
        {
            Id = Guid.CreateVersion7(),
            UserId = data.User.Id,
            OldEmail = data.User.Email,
            NewEmail = lowerCaseEmail,
            TokenHash = HashingUtils.HashToken(token),
            SecurityStampAtCreate = data.User.SecurityStamp
        };

        // Dispatch the verification email *before* committing the row. If the mail service throws
        // (provider outage, transient network failure), the exception propagates and the row is
        // never inserted, the user can simply retry without burning a pending-count slot.
        await _emailService.VerifyEmail(new Contact(lowerCaseEmail, data.User.Name),
            new Uri(_frontendConfig.BaseUrl, $"/verify-email?token={token}"));

        _db.UserEmailChanges.Add(emailChange);
        await _db.SaveChangesAsync();

        // Notify the previous address so the legitimate owner sees the change request even if
        // the session/password used to start it was compromised. Best-effort: the verification
        // email has already been dispatched and the row is committed, so a failure here must not
        // unwind the request.
        try
        {
            await _emailService.EmailChangeNotice(new Contact(data.User.Email, data.User.Name), lowerCaseEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email-change notice to previous address for user {UserId}", data.User.Id);
        }

        return new Success();
    }

    public async Task<OneOf<Success, NotFound, EmailAlreadyInUse>> TryVerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = HashingUtils.HashToken(token);
        var validSince = DateTime.UtcNow - Duration.EmailChangeRequestLifetime;

        var change = await _db.UserEmailChanges
            .Where(x => x.TokenHash == hash && x.UsedAt == null && x.CreatedAt >= validSince
                && x.SecurityStampAtCreate == x.User.SecurityStamp
                && x.User.UserDeactivation == null && x.User.ActivatedAt != null)
            .Select(x => new
            {
                ChangeId = x.Id,
                UserId = x.UserId,
                x.NewEmail,
                x.SecurityStampAtCreate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (change is null) return new NotFound();

        // Race-safe consume + apply: only updates if SecurityStamp still matches the snapshot, so
        // sibling email changes / password resets that completed since the read above cleanly lose.
        var newStamp = Guid.CreateVersion7();
        try
        {
            var userRows = await _db.Users
                .Where(u => u.Id == change.UserId && u.SecurityStamp == change.SecurityStampAtCreate)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Email, change.NewEmail)
                    .SetProperty(u => u.SecurityStamp, newStamp), cancellationToken);
            if (userRows == 0) return new NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Another account claimed this email between request creation and verification.
            // The pending row stays as-is (not marked used) so it can expire naturally.
            return new EmailAlreadyInUse();
        }

        var now = DateTime.UtcNow;
        await _db.UserEmailChanges
            .Where(c => c.Id == change.ChangeId && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, now), cancellationToken);

        return new Success();
    }

    private async Task<bool> CheckPassword(string password, User user)
    {
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return false;
        }
        
        var result = HashingUtils.VerifyPassword(password, user.PasswordHash);

        if (!result.Verified)
        {
            _logger.LogInformation("Failed to verify password for user ID: [{Id}]", user.Id);
            return false;
        }

        if (result.NeedsRehash)
        {
            // Re-hashing the same password to upgrade algorithms intentionally does not rotate
            // SecurityStamp, the credential value is unchanged, so outstanding reset / email-change
            // links for this user must continue to work.
            _logger.LogInformation("Rehashing password for user ID: [{Id}]", user.Id);
            user.PasswordHash = HashingUtils.HashPassword(password);
            await _db.SaveChangesAsync();
        }

        return true;
    }
}