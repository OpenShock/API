using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Results;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;
using OpenShock.Common.Validation;
using OpenShock.Internal.Common.Utils;

namespace OpenShock.API.Services.Account;

/// <summary>
/// Default implementation of IAccountService
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly OpenShockContext _db;
    private readonly IRedisPubService _redisPubService;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly IAutomationTokenService _automationTokens;
    private readonly MailOptions _mailOptions;
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="redisPubService">Used to notify the email outbox delivery job that mail was enqueued.</param>
    /// <param name="sessionService"></param>
    /// <param name="auditService"></param>
    /// <param name="automationTokens">The automation token this request carries, if any; an account it creates is linked to it.</param>
    /// <param name="mailOptions">Decides whether an activation link can ever reach the user.</param>
    /// <param name="logger"></param>
    public AccountService(OpenShockContext db, IRedisPubService redisPubService, ISessionService sessionService, IAuditService auditService, IAutomationTokenService automationTokens, MailOptions mailOptions, ILogger<AccountService> logger)
    {
        _db = db;
        _redisPubService = redisPubService;
        _sessionService = sessionService;
        _auditService =  auditService;
        _automationTokens = automationTokens;
        _mailOptions = mailOptions;
        _logger = logger;
    }

    /// <summary>
    /// Seeds a random token hash for a freshly created request row. The plaintext is discarded
    /// immediately: the email outbox delivery job mints the real token (and overwrites this hash) when it
    /// sends, so this value is never the one delivered. It exists only to keep the column populated.
    /// </summary>
    private static string SeedTokenHash()
        => HashingUtils.HashToken(CryptoUtils.RandomString(AuthConstants.GeneratedTokenLength));

    /// <summary>
    /// Best-effort nudge to the email outbox delivery job that a message was enqueued. Deliberately
    /// swallows failures: the outbox row is already committed, so a dropped notification only delays
    /// delivery to the next scheduled delivery sweep, and a transient Redis hiccup must not fail a request
    /// whose work already succeeded.
    /// </summary>
    private async Task NotifyEmailOutboxAsync()
    {
        try
        {
            await _redisPubService.SendEmailOutboxPending();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify the email outbox delivery job; delivery will fall back to the scheduled sweep");
        }
    }

    /// <summary>Most activation emails one account, and one address, is sent per <see cref="Duration.ActivationResendWindow"/>.</summary>
    private const int MaxActivationEmailsPerWindow = 5;

    /// <summary>
    /// Whether <paramref name="email"/> has already been sent as many activation emails as an address gets
    /// per window, whichever accounts they were for. The cap per account alone does not protect an inbox:
    /// a pending account can be replaced by a new signup, and every new account starts counting from zero.
    /// Counted on the outbox, which is never pruned, so it holds across instances and deleted accounts.
    /// </summary>
    /// <param name="email">Lowercased address.</param>
    /// <param name="cancellationToken"></param>
    private async Task<bool> IsActivationEmailCapReachedAsync(string email, CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow - Duration.ActivationResendWindow;
        var sent = await _db.EmailOutbox.CountAsync(
            m => m.Recipient == email && m.Type == EmailType.AccountActivation && m.CreatedAt >= windowStart,
            cancellationToken);

        return sent >= MaxActivationEmailsPerWindow;
    }

    /// <summary>The activation emails queued for one account within <see cref="Duration.ActivationResendWindow"/>.</summary>
    private IQueryable<EmailOutboxMessage> RecentActivationEmails(Guid userId)
    {
        var coalesceKey = EmailOutboxCoalesceKeys.AccountActivation(userId);
        var windowStart = DateTime.UtcNow - Duration.ActivationResendWindow;
        return _db.EmailOutbox.Where(m => m.CoalesceKey == coalesceKey && m.CreatedAt >= windowStart);
    }

    private Task<int> CountRecentActivationEmailsAsync(Guid userId, CancellationToken cancellationToken)
        => RecentActivationEmails(userId).CountAsync(cancellationToken);

    private Task<bool> IsUserNameBlacklisted(string username, CancellationToken cancellationToken)
        => _db.UserNameBlacklists.Matching(username).AnyAsync(cancellationToken);

    private async Task<bool> IsEmailProviderBlacklisted(string email, CancellationToken cancellationToken)
    {
        if (!EmailValidator.TrySplitAddress(email, out _, out var domainSpan)) return false;
        var domain = domainSpan.ToString();
        return await _db.EmailProviderBlacklists.AnyAsync(e => e.Domain == domain, cancellationToken);
    }

    private async Task<AccountCreationResult> CreateAccount(string email, string username, string password, bool verifyOnCreation, Guid? createdByAutomationTokenId, CancellationToken cancellationToken)
    {
        email = email.ToLowerInvariant();

        if (await IsUserNameBlacklisted(username, cancellationToken) || await IsEmailProviderBlacklisted(email, cancellationToken))
            return new AccountWithEmailOrUsernameExists();
        
        // This signup would mail the address (unless accounts are activated on creation). Past the cap it is
        // turned away like a taken address, before it can replace whoever holds the address now.
        if (!verifyOnCreation && await IsActivationEmailCapReachedAsync(email, cancellationToken))
            return new AccountWithEmailOrUsernameExists();

        await ReleaseEmailFromPendingAccountAsync(email, claimantOwnsEmail: false, exceptUserId: null, cancellationToken);

        if (await _db.Users.AnyAsync(x => x.Email == email || x.Name == username, cancellationToken))
            return new AccountWithEmailOrUsernameExists();

        var user = new User
        {
            Name = username,
            Email = email,
            PasswordHash = HashingUtils.HashPassword(password),
            CreatedByAutomationTokenId = createdByAutomationTokenId
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

        return user;
    }

    /// <inheritdoc />
    public async Task<AccountCreationResult> CreateAccountWithActivationFlowAsync(string email, string username, string password, CancellationToken cancellationToken = default)
    {
        // With mail disabled the activation email is never delivered, so an activation flow would leave
        // the account permanently unusable. Activate on creation instead.
        var mailEnabled = _mailOptions.IsEnabled;

        // An account an automation token creates must never exist without the audit entry for that use, so the
        // account, its activation request and the audit entry commit together.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Created with an automation token, the account is linked to it on insert, which makes it an automated account.
        var accountCreate = await CreateAccount(email, username, password, !mailEnabled, _automationTokens.Current?.Id, cancellationToken);
        if (accountCreate.Value is not User user) return accountCreate;

        if (mailEnabled)
        {
            // The real activation token is minted by the outbox delivery job at send time; here we record the
            // request (with a seeded hash) and durably enqueue the email.
            user.UserActivationRequest = new UserActivationRequest
            {
                UserId = user.Id,
                TokenHash = SeedTokenHash()
            };

            _db.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(user.Id, user.Email, username));

            await _db.SaveChangesAsync();
        }

        await _automationTokens.RecordSignupAsync(user.Id);

        await transaction.CommitAsync();

        if (mailEnabled) await NotifyEmailOutboxAsync();

        return user;
    }

    public Task<bool> IsEmailRegisteredAsync(string email, CancellationToken cancellationToken = default)
    {
        return _db.Users.AnyAsync(u => u.Email == email && u.ActivatedAt != null, cancellationToken);
    }

    /// <summary>
    /// A never activated account is only a pending signup: nobody has shown the address is theirs, so it
    /// must not keep the real owner (or anyone else) from signing up with that address. This deletes the
    /// pending account holding <paramref name="email"/>, if there is one and it may be released.
    /// </summary>
    /// <param name="email">Lowercased address.</param>
    /// <param name="claimantOwnsEmail">
    /// The caller proved the address is theirs (an OAuth provider verified it, or they opened a link mailed
    /// to it). Otherwise the pending account is left alone for <see cref="Duration.PendingSignupHoldTime"/>
    /// after it was created or last mailed: that gives its creator time to receive and open the link before
    /// someone who merely knows the address can take the signup away, and keeps repeated signups from
    /// mailing the address more often than a resend could.
    /// </param>
    /// <param name="exceptUserId">A pending account that is the caller's own and must be kept.</param>
    /// <param name="cancellationToken"></param>
    private async Task ReleaseEmailFromPendingAccountAsync(string email, bool claimantOwnsEmail, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        var pending = await _db.Users
            .AsNoTracking()
            .Where(u => u.Email == email && u.ActivatedAt == null)
            .Select(u => new { u.Id, u.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (pending is null || pending.Id == exceptUserId) return;

        if (!claimantOwnsEmail)
        {
            // A resend takes this same lock before it queues mail, so one cannot slip in between the look at
            // the outbox below and the delete. Held until the caller's transaction ends.
            await _db.Database.ExecuteSqlAsync($"SELECT 1 FROM users WHERE id = {pending.Id} FOR UPDATE", cancellationToken);

            // Counted from the last activation mail, not only from signup: a pending account that just had
            // its mail resent would otherwise be replaced by one that mails the same inbox again right away.
            var holdStart = DateTime.UtcNow - Duration.PendingSignupHoldTime;
            if (pending.CreatedAt > holdStart) return;

            var coalesceKey = EmailOutboxCoalesceKeys.AccountActivation(pending.Id);
            if (await _db.EmailOutbox.AnyAsync(m => m.CoalesceKey == coalesceKey && m.CreatedAt >= holdStart, cancellationToken)) return;
        }

        // Only while it still holds this address: its creator may have moved it to another one since the
        // lookup above, and then it is not in the way any more.
        await DeletePendingAccountAsync(pending.Id, email, cancellationToken);
    }

    /// <summary>
    /// Deletes an account only if it is still pending when the delete runs. The condition is part of the
    /// statement, so an account that gets activated after the caller looked at it is left alone.
    /// Deactivated and privileged accounts are never deleted this way.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="heldEmail">When given, the account is only deleted while this is still its address.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>False when nothing was deleted.</returns>
    private async Task<bool> DeletePendingAccountAsync(Guid userId, string? heldEmail, CancellationToken cancellationToken)
    {
        var deleted = heldEmail is null
            ? await _db.Database.ExecuteSqlAsync($"""
                DELETE FROM users AS u
                WHERE u.id = {userId}
                  AND u.activated_at IS NULL
                  AND NOT (u.roles && {PrivilegedRoles.All})
                  AND NOT EXISTS (SELECT 1 FROM user_deactivations AS d WHERE d.deactivated_user_id = u.id)
                """, cancellationToken)
            : await _db.Database.ExecuteSqlAsync($"""
                DELETE FROM users AS u
                WHERE u.id = {userId}
                  AND u.email = {heldEmail}
                  AND u.activated_at IS NULL
                  AND NOT (u.roles && {PrivilegedRoles.All})
                  AND NOT EXISTS (SELECT 1 FROM user_deactivations AS d WHERE d.deactivated_user_id = u.id)
                """, cancellationToken);

        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<Union4<User, AccountWithEmailOrUsernameExists, TooManyActivationResends, NotFound>> RestartPendingSignupAsync(Guid userId, string email, string username, CancellationToken cancellationToken = default)
    {
        email = email.ToLowerInvariant();

        // Everything below, the "still pending" check included, runs with the account's row locked. An
        // activation that commits first is seen here; one that comes later waits and then finds its token
        // replaced. Releasing another pending account's hold on the address is part of the same
        // transaction, so every return that does not commit rolls it back.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Releasing the address locks the pending account that holds it as well. Both rows are taken here,
        // in id order: two signups each moving onto the other's address would otherwise lock them in
        // opposite order and deadlock.
        var holderId = await _db.Users
            .Where(u => u.Email == email && u.ActivatedAt == null && u.Id != userId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Guid[] lockIds = holderId is null ? [userId] : [userId, holderId.Value];
        await _db.Database.ExecuteSqlAsync($"SELECT 1 FROM users WHERE id = ANY({lockIds}) ORDER BY id FOR UPDATE", cancellationToken);

        var user = await _db.Users
            .Include(u => u.UserDeactivation)
            .Include(u => u.UserActivationRequest)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return new NotFound();

        // The caller usually looked this account up through this same context already, and a query hands
        // back that tracked instance as it was read then. Read the row as it is now.
        await _db.Entry(user).ReloadAsync(cancellationToken);
        if (user.ActivatedAt is not null || user.UserDeactivation is not null) return new NotFound();

        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged || user.Name != username)
        {
            if (await IsUserNameBlacklisted(username, cancellationToken) || await IsEmailProviderBlacklisted(email, cancellationToken))
                return new AccountWithEmailOrUsernameExists();

            if (emailChanged)
            {
                // Moving the signup to another address kills the link already mailed, so the new address
                // has to get one. Checked before anything changes: past the account's cap no mail would be
                // queued below, leaving an account that reports success and has no working link at all.
                if (await CountRecentActivationEmailsAsync(userId, cancellationToken) >= MaxActivationEmailsPerWindow)
                    return new TooManyActivationResends();

                // Moving the signup to another address mails that address.
                if (await IsActivationEmailCapReachedAsync(email, cancellationToken))
                    return new AccountWithEmailOrUsernameExists();
            }

            await ReleaseEmailFromPendingAccountAsync(email, claimantOwnsEmail: false, exceptUserId: userId, cancellationToken);

            if (await _db.Users.AnyAsync(u => u.Id != userId && (u.Email == email || u.Name == username), cancellationToken))
                return new AccountWithEmailOrUsernameExists();

            user.Email = email;
            user.Name = username;

            // A link already mailed was mailed to the previous address, so it says nothing about the new
            // one: it must stop working in the same commit that changes the address. Otherwise the caller
            // could sign up with an inbox of their own, move the account to somebody else's address and
            // activate it with the link they already hold. The delivery job mints the new token when it
            // sends to the new address.
            if (emailChanged)
            {
                if (user.UserActivationRequest is not null)
                {
                    user.UserActivationRequest.TokenHash = SeedTokenHash();
                }
                else
                {
                    // The delivery job skips an activation mail that has no request to mint a token on.
                    user.UserActivationRequest = new UserActivationRequest
                    {
                        UserId = user.Id,
                        TokenHash = SeedTokenHash(),
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // The same goes for a password reset link requested for the previous address: completing it
                // would set a password and activate the account on the new one. Reset rows carry the stamp
                // they were created under, so rotating it invalidates them.
                user.SecurityStamp = Guid.CreateVersion7();

                _db.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(user.Id, user.Email, user.Name));
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // The old link was redeemed while this ran: the account is activated on the address it was
                // mailed to, and nothing was changed here.
                return new NotFound();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                return new AccountWithEmailOrUsernameExists();
            }
        }

        await transaction.CommitAsync(cancellationToken);

        if (emailChanged)
        {
            // The replacement mail is part of the same transaction as the address and token change.
            await NotifyEmailOutboxAsync();
        }
        else
        {
            // For unchanged details a throttled result just means no new mail went out: the link already
            // mailed still works, and the resend button gets a new one later.
            await ResendActivationEmailCoreAsync(userId, ignoreCooldown: false, onlyIfEmailIs: null, reserveCorrectionCapacity: false, cancellationToken);
        }

        return user;
    }

    public async Task<AccountCreationResult> CreateOAuthOnlyAccountAsync(
        string email,
        string username,
        string provider,
        string providerAccountId,
        string? providerAccountName,
        bool isEmailTrusted,
        Guid? replacePendingUserId,
        CancellationToken cancellationToken)
    {
        email = email.ToLowerInvariant();
        provider = provider.ToLowerInvariant();

        // Only a provider that verified the address shows it is the caller's. Mail being disabled (below)
        // makes the new account usable without a link, it proves nothing about who owns the address.
        var providerVerifiedEmail = isEmailTrusted;

        // With mail disabled no activation link can reach the user, so an untrusted email cannot be
        // verified by any means; activate on creation rather than create a dead account.
        isEmailTrusted |= !_mailOptions.IsEnabled;

        // Reuse your existing guards
        if (await IsUserNameBlacklisted(username, cancellationToken) || await IsEmailProviderBlacklisted(email, cancellationToken))
            return new AccountWithEmailOrUsernameExists();

        // An unverified address gets an activation email.
        if (!isEmailTrusted && await IsActivationEmailCapReachedAsync(email, cancellationToken))
            return new AccountWithEmailOrUsernameExists();

        // Pending accounts removed to make room only stay removed if the new account is created: every
        // return below that does not commit rolls them back.
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (replacePendingUserId is not null)
            {
                await DeletePendingAccountAsync(replacePendingUserId.Value, heldEmail: null, cancellationToken);

                // The caller looked that account up through this context, and its OAuth connection has the
                // key the new one is about to get. Nothing is tracked yet that this method needs.
                _db.ChangeTracker.Clear();
            }

            // A provider-verified address belongs to this caller, so a pending account someone parked on it goes.
            await ReleaseEmailFromPendingAccountAsync(email, claimantOwnsEmail: providerVerifiedEmail, exceptUserId: null, cancellationToken);

            // Fast uniqueness check (optimistic; race handled by unique constraints below)
            var exists = await _db.Users.AnyAsync(u => u.Email == email || u.Name == username, cancellationToken);
            if (exists) return new AccountWithEmailOrUsernameExists();

            var creationTime = DateTime.UtcNow;
            
            var user = new User
            {
                Name = username,
                Email = email,
                PasswordHash = null,
                CreatedAt = creationTime,
                ActivatedAt = isEmailTrusted ? creationTime : null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // If email isn't trusted, create an activation request and durably enqueue the activation
            // email. The token itself is minted by the outbox delivery job at send time.
            if (!isEmailTrusted)
            {
                user.UserActivationRequest = new UserActivationRequest
                {
                    UserId = user.Id,
                    TokenHash = SeedTokenHash(),
                    CreatedAt = creationTime
                };

                _db.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(user.Id, email, username));

                await _db.SaveChangesAsync();
            }

            // Link external identity
            _db.UserOAuthConnections.Add(new UserOAuthConnection
            {
                UserId = user.Id,
                ProviderKey = provider,
                ExternalId = providerAccountId,
                DisplayName = providerAccountName?.Truncate(ApiHardLimits.OAuthDisplayNameMaxLength),
                CreatedAt = creationTime,
            });

            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // Notify the outbox delivery job only after a successful commit.
            if (!isEmailTrusted)
            {
                await NotifyEmailOutboxAsync();
            }

            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            await tx.RollbackAsync();
            return new AccountWithEmailOrUsernameExists();
        }
    }


    /// <inheritdoc />
    public async Task<Union3<Success, ActivationCredentialRequired, NotFound>> ActivateAccountAsync(string activationToken, Guid? signupBrowserUserId, string? password, CancellationToken cancellationToken = default)
    {
        var hash = HashingUtils.HashToken(activationToken);

        var user = await _db.Users
            .Include(u => u.UserActivationRequest)
            .FirstOrDefaultAsync(x => x.ActivatedAt == null && x.UserDeactivation == null && x.UserActivationRequest != null && x.UserActivationRequest.TokenHash == hash, cancellationToken);
        if (user?.UserActivationRequest is null) return new NotFound();

        // The token proves the caller reads this inbox, nothing more: the account, and the password or OAuth
        // identity on it, may have been set up by someone else using this address. So the caller must also
        // be the one who signed up: the browser that did, or someone who knows the password.
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        var credentialProven = signupBrowserUserId == user.Id;
        var passwordInvalid = false;
        if (!credentialProven && hasPassword && !string.IsNullOrEmpty(password))
        {
            credentialProven = HashingUtils.VerifyPassword(password, user.PasswordHash!).Verified;
            passwordInvalid = !credentialProven;
        }

        if (!credentialProven)
        {
            var providers = await _db.UserOAuthConnections
                .Where(c => c.UserId == user.Id)
                .Select(c => c.ProviderKey)
                .ToListAsync(cancellationToken);

            return new ActivationCredentialRequired(hasPassword, providers, passwordInvalid);
        }

        var activated = await ConsumeActivationTokenAsync(user.Id, hash, cancellationToken);
        if (!activated) return new NotFound();

        return new Success();
    }

    /// <summary>
    /// Activates the account by consuming its activation request, but only if the request still carries
    /// <paramref name="tokenHash"/>. The check and the delete are one statement, so a token that was
    /// replaced since the caller looked it up (the signup's address was changed, see
    /// <see cref="RestartPendingSignupAsync"/>) cannot activate the account on its new address.
    /// </summary>
    private async Task<bool> ConsumeActivationTokenAsync(Guid userId, string tokenHash, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // User row first, then the activation request: the order password reset, resend and signup
        // correction take them in. Taking the request first could deadlock against any of those.
        await _db.Database.ExecuteSqlAsync($"SELECT 1 FROM users WHERE id = {userId} FOR UPDATE", cancellationToken);

        var canActivate = await _db.Users
            .AnyAsync(u => u.Id == userId && u.ActivatedAt == null && u.UserDeactivation == null, cancellationToken);
        if (!canActivate) return false;

        var consumed = await _db.UserActivationRequests
            .Where(r => r.UserId == userId && r.TokenHash == tokenHash)
            .ExecuteDeleteAsync(cancellationToken);
        if (consumed == 0) return false;

        var now = DateTime.UtcNow;
        await _db.Users
            .Where(u => u.Id == userId && u.ActivatedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActivatedAt, now), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryActivateAccountAsync(Guid userId, string activationToken, CancellationToken cancellationToken = default)
    {
        var hash = HashingUtils.HashToken(activationToken);

        var matches = await _db.Users
            .AnyAsync(x => x.Id == userId && x.ActivatedAt == null && x.UserDeactivation == null && x.UserActivationRequest != null && x.UserActivationRequest.TokenHash == hash, cancellationToken);
        if (!matches) return false;

        return await ConsumeActivationTokenAsync(userId, hash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Union4<Success, TooManyActivationResends, AccountDeactivated, NotFound>> ResendActivationEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var userId = await _db.Users
            .Where(u => u.Email == usernameOrEmail || u.Name == usernameOrEmail)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is null) return new NotFound();

        return await ResendActivationEmailAsync(userId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Union4<Success, TooManyActivationResends, AccountDeactivated, NotFound>> ResendActivationEmailAsync(Guid userId, CancellationToken cancellationToken = default)
        => ResendActivationEmailCoreAsync(userId, ignoreCooldown: false, onlyIfEmailIs: null, reserveCorrectionCapacity: false, cancellationToken);

    /// <inheritdoc />
    public Task<Union4<Success, TooManyActivationResends, AccountDeactivated, NotFound>> ResendActivationEmailForOAuthAsync(Guid userId, CancellationToken cancellationToken = default)
        => ResendActivationEmailCoreAsync(userId, ignoreCooldown: false, onlyIfEmailIs: null, reserveCorrectionCapacity: true, cancellationToken);

    /// <param name="userId"></param>
    /// <param name="ignoreCooldown"></param>
    /// <param name="onlyIfEmailIs">When given, nothing is queued unless this is still the account's address.</param>
    /// <param name="reserveCorrectionCapacity">
    /// Stop one short of the account's cap, so a resend nobody asked for cannot use up the send that
    /// correcting the signup's address needs.
    /// </param>
    /// <param name="cancellationToken"></param>
    private async Task<Union4<Success, TooManyActivationResends, AccountDeactivated, NotFound>> ResendActivationEmailCoreAsync(Guid userId, bool ignoreCooldown, string? onlyIfEmailIs, bool reserveCorrectionCapacity, CancellationToken cancellationToken)
    {
        // Without mail no link can be delivered (and such accounts are activated on creation anyway).
        if (!_mailOptions.IsEnabled) return new NotFound();

        // Serialize resends per user: lock the user row for the whole check-then-enqueue, so concurrent
        // requests can't both pass the limit below. Released on commit/rollback.
        await using var tx = await _db.Database.BeginTransactionAsync();
        await _db.Database.ExecuteSqlAsync($"SELECT 1 FROM users WHERE id = {userId} FOR UPDATE", cancellationToken);

        var user = await _db.Users
            .Include(u => u.UserActivationRequest)
            .Include(u => u.UserDeactivation)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return new NotFound();

        // An instance this context already tracks comes back as it was first read, so read the locked row
        // as it is now: the address below is the one that gets mailed and counted.
        await _db.Entry(user).ReloadAsync(cancellationToken);
        if (user.ActivatedAt is not null) return new NotFound();
        if (user.UserDeactivation is not null) return new AccountDeactivated();
        if (onlyIfEmailIs is not null && !string.Equals(user.Email, onlyIfEmailIs, StringComparison.OrdinalIgnoreCase)) return new Success();

        // Throttle on the outbox itself (it is never auto-pruned), so the limit holds across API instances
        // and restarts. The signup email counts towards it.
        var recent = await RecentActivationEmails(userId)
            .Select(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var maxRecent = reserveCorrectionCapacity ? MaxActivationEmailsPerWindow - 1 : MaxActivationEmailsPerWindow;
        if (recent.Count >= maxRecent || (!ignoreCooldown && recent.Any(createdAt => createdAt >= DateTime.UtcNow - Duration.ActivationResendCooldown)))
            return new TooManyActivationResends();

        if (await IsActivationEmailCapReachedAsync(user.Email, cancellationToken))
            return new TooManyActivationResends();

        user.UserActivationRequest ??= new UserActivationRequest
        {
            UserId = user.Id,
            TokenHash = SeedTokenHash(),
            CreatedAt = DateTime.UtcNow
        };

        _db.EmailOutbox.Add(EmailOutboxMessage.ForAccountActivation(user.Id, user.Email, user.Name));
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // Notify the outbox delivery job only after a successful commit.
        await NotifyEmailOutboxAsync();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union5<Success, CannotDeactivatePrivilegedAccount, AccountDeactivationAlreadyInProgress, Unauthorized, NotFound>> DeactivateAccountAsync(Guid executingUserId, Guid userId, bool deleteLater, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (executingUserId != userId)
        {
            var isPrivileged = await _db.Users
                            .Where(u => u.Id == executingUserId)
                            .SelectMany(u => u.Roles)
                            .AnyAsync(r =>
                                r == RoleType.Staff ||
                                r == RoleType.Admin ||
                                r == RoleType.System, cancellationToken);
            if (!isPrivileged)
            {
                return new Unauthorized();
            }
        }

        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return new NotFound();

        if (user.Roles.Any(r => r is RoleType.Admin or RoleType.System))
        {
            return new CannotDeactivatePrivilegedAccount();
        }

        if (user.UserDeactivation is not null)
        {
            return new AccountDeactivationAlreadyInProgress();
        }

        await using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            user.UserDeactivation = new UserDeactivation
            {
                DeactivatedUserId = userId,
                DeactivatedByUserId = executingUserId,
                DeleteLater = deleteLater,
            };
            
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                action: AuditAction.AccountDeactivated,
                actorId: executingUserId,
                metadata: new AccountDeactivatedMetadata(deleteLater),
                reason: reason
            );

            await transaction.CommitAsync();
        }

        // Remove all login sessions
        await _sessionService.DeleteSessionsByUserIdAsync(userId);

        await RefreshOwnedDevicesAsync(userId, cancellationToken);

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union3<Success, Unauthorized, NotFound>> ReactivateAccountAsync(Guid executingUserId, Guid userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(u => u.Id == userId && u.UserDeactivation != null, cancellationToken);
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
                                r == RoleType.System, cancellationToken);
            if (!isPrivileged)
            {
                return new Unauthorized();
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Remove(deactivation);

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            action: AuditAction.AccountReactivated,
            actorId: executingUserId,
            reason: reason
        );

        await transaction.CommitAsync();

        await RefreshOwnedDevicesAsync(userId, cancellationToken);

        return new Success();
    }

    private async Task RefreshOwnedDevicesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var deviceIds = await _db.Devices.Where(x => x.OwnerId == userId).Select(x => x.Id).ToArrayAsync(cancellationToken);
        await Task.WhenAll(deviceIds.Select(_redisPubService.SendDeviceUpdate));
    }

    /// <inheritdoc />
    public async Task<Union4<Success, CannotDeletePrivilegedAccount, Unauthorized, NotFound>> DeleteAccountAsync(Guid executingUserId, Guid userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var isPrivileged = await _db.Users
                        .Where(u => u.Id == executingUserId)
                        .SelectMany(u => u.Roles)
                        .AnyAsync(r =>
                            r == RoleType.Staff ||
                            r == RoleType.Admin ||
                            r == RoleType.System, cancellationToken);
        if (!isPrivileged)
        {
            return new Unauthorized();
        }

        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return new NotFound();

        if (user.Roles.Any(r => r is RoleType.Admin or RoleType.System))
        {
            return new CannotDeletePrivilegedAccount();
        }

        // TODO: Do more checks?

        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.Users.Remove(user);

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            action: AuditAction.AccountDeleted,
            actorId: executingUserId,
            reason: reason
        );

        await transaction.CommitAsync();

        return new Success();
    }

    /// <inheritdoc/>
    public async Task<Union5<User, NotFound, AccountDeactivated, AccountNotActivated, AccountIsOAuthOnly>> GetAccountByCredentialsAsync(string usernameOrEmail, string password,  CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserDeactivation)
            .FirstOrDefaultAsync(x => x.Email == usernameOrEmail || x.Name == usernameOrEmail, cancellationToken);
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
    public async Task<Union3<Success, NotFound, SecretInvalid>> CheckPasswordResetExistsAsync(Guid passwordResetId, string secret,
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
    public async Task<Union4<Success, TooManyPasswordResets, AccountDeactivated, NotFound>> CreatePasswordResetFlowAsync(string email, CancellationToken cancellationToken)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var user = await _db.Users
            .Where(x => x.Email == email)
            .Select(x => new
            {
                User = x,
                IsDeactivated = x.UserDeactivation != null,
                PasswordResetCount = x.PasswordResets.Count(y => y.UsedAt == null && y.CreatedAt >= validSince)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null) return new NotFound();
        // Unactivated accounts are allowed: completing the reset proves control of the inbox, which is all
        // activation checks, so it activates the account (see CompletePasswordResetFlowAsync).
        if (user.IsDeactivated) return new AccountDeactivated();
        if (user.PasswordResetCount >= 3) return new TooManyPasswordResets();

        // The reset token is minted lazily by the outbox delivery job at send time (and re-minted on any
        // resend), so no usable reset link is ever stored. Here we only record the request and
        // durably enqueue the email.
        var passwordReset = new UserPasswordReset
        {
            UserId = user.User.Id,
            TokenHash = SeedTokenHash(),
            SecurityStampAtCreate = user.User.SecurityStamp
        };
        _db.UserPasswordResets.Add(passwordReset);
        _db.EmailOutbox.Add(EmailOutboxMessage.ForPasswordReset(passwordReset.Id, user.User.Id, user.User.Email, user.User.Name));
        await _db.SaveChangesAsync();
        await NotifyEmailOutboxAsync();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union4<Success, NotFound, AccountDeactivated, SecretInvalid>> CompletePasswordResetFlowAsync(Guid passwordResetId,
        string secret, string newPassword,
        CancellationToken cancellationToken)
    {
        var validSince = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;

        var reset = await _db.UserPasswordResets
            .Select(x => new
            {
                Reset = x,
                IsDeactivated = x.User.UserDeactivation != null,
                UserSecurityStamp = x.User.SecurityStamp
            })
            .FirstOrDefaultAsync(x => x.Reset.Id == passwordResetId && x.Reset.UsedAt == null && x.Reset.CreatedAt >= validSince
                && x.Reset.SecurityStampAtCreate == x.UserSecurityStamp, cancellationToken);
        if (reset is null) return new NotFound();
        if (reset.IsDeactivated) return new AccountDeactivated();

        var result = HashingUtils.VerifyToken(secret, reset.Reset.TokenHash);
        if (!result.Verified) return new SecretInvalid();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Whether this reset is what activates the account decides below if its OAuth links are dropped.
        // Activating does not rotate the security stamp, so the stamp check in the update cannot tell that
        // the account was activated since the read above: lock the row, as activation does, and look now.
        await _db.Database.ExecuteSqlAsync($"SELECT 1 FROM users WHERE id = {reset.Reset.UserId} FOR UPDATE", cancellationToken);
        var wasActivated = await _db.Users
            .Where(u => u.Id == reset.Reset.UserId)
            .Select(u => u.ActivatedAt != null)
            .FirstOrDefaultAsync(cancellationToken);

        // Race-safe consume + apply: only updates if SecurityStamp still matches the snapshot.
        // If a sibling reset (or a separate password/email change) completed since the read above,
        // the stamp has rotated and the predicate matches zero rows.
        // The reset link was delivered to the account's email, so completing it also verifies that email:
        // an unactivated account is activated here (an already activated one keeps its original timestamp).
        var newPasswordHash = HashingUtils.HashPassword(newPassword);
        var newStamp = Guid.CreateVersion7();
        var now = DateTime.UtcNow;
        var userRows = await _db.Users
            .Where(u => u.Id == reset.Reset.UserId && u.SecurityStamp == reset.Reset.SecurityStampAtCreate)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, newPasswordHash)
                .SetProperty(u => u.SecurityStamp, newStamp)
                .SetProperty(u => u.ActivatedAt, u => u.ActivatedAt ?? now));
        if (userRows == 0) return new NotFound();

        // Any pending activation request is moot now; dropping it also makes the outbox skip a queued activation email.
        await _db.UserActivationRequests
            .Where(r => r.UserId == reset.Reset.UserId)
            .ExecuteDeleteAsync();

        await _db.UserPasswordResets
            .Where(r => r.Id == reset.Reset.Id && r.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UsedAt, now));

        // This reset is what activates a not yet activated account. Such an account may have been created
        // by someone else on this address through an OAuth signup, and its linked identity is then theirs:
        // left in place it would let them sign in to the account the inbox owner just took over. Nobody
        // has proven that link and this inbox belong together, so it goes. (An unactivated account cannot
        // sign in, so it has no sessions or API tokens to worry about.)
        // On an activated account the links are kept, like API tokens: activating required proving both
        // the inbox and the credential, so every link on it was added by its owner.
        List<string> removedProviders = [];
        if (!wasActivated)
        {
            var connections = _db.UserOAuthConnections.Where(c => c.UserId == reset.Reset.UserId);
            removedProviders = await connections.Select(c => c.ProviderKey).ToListAsync();
            if (removedProviders.Count > 0)
            {
                await connections.ExecuteDeleteAsync();
            }
        }

        // Unauthenticated email-token flow proving control of the account, so the actor is the account itself.
        await _auditService.LogAsync(
            reset.Reset.UserId,
            action: AuditAction.PasswordChanged,
            actorId: reset.Reset.UserId
        );

        foreach (var provider in removedProviders)
        {
            await _auditService.LogAsync(
                reset.Reset.UserId,
                action: AuditAction.OAuthDisconnected,
                actorId: reset.Reset.UserId,
                metadata: new OAuthDisconnectedMetadata(provider),
                reason: "Password reset"
            );
        }

        // Whoever was signed in with the old password is signed out. Sessions live in Redis, outside this
        // transaction, so they go first: if that fails nothing is committed and the link can be used again,
        // rather than the reset succeeding with the old sessions left alive and no way to retry.
        await _sessionService.DeleteSessionsByUserIdAsync(reset.Reset.UserId);

        await transaction.CommitAsync();

        // A sign-in with the old password may have slipped in between the two steps above. The reset is
        // committed and its link used up, so failing the request here would report an error for a password
        // that did change, with nothing left to retry: try the sign-out again instead, and give up loudly.
        const int signOutAttempts = 3;
        for (var attempt = 1; attempt <= signOutAttempts; attempt++)
        {
            try
            {
                await _sessionService.DeleteSessionsByUserIdAsync(reset.Reset.UserId);
                break;
            }
            catch (Exception ex) when (attempt < signOutAttempts)
            {
                _logger.LogWarning(ex, "Failed to re-check sessions after password reset for user ID: [{Id}], retrying", reset.Reset.UserId);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-check sessions after password reset for user ID: [{Id}]; a session created during the reset may still be alive", reset.Reset.UserId);
            }
        }

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union2<Success, EmailTaken>> CheckEmailAvailability(string email,
        CancellationToken cancellationToken = default)
    {
        // A never activated account is only a pending signup and does not reserve its address.
        var isTaken = await _db.Users.AnyAsync(x => x.Email == email && x.ActivatedAt != null, cancellationToken: cancellationToken);
        if (isTaken) return new EmailTaken();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union3<Success, UsernameTaken, UsernameError>> CheckUsernameAvailabilityAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var validationResult = UsernameValidator.Validate(username);
        if (validationResult.Value is UsernameError usernameError)
            return usernameError;

        if (await IsUserNameBlacklisted(username, cancellationToken))
            return new UsernameTaken(); // Don't inform the user about when the blacklist is hit

        var isTaken = await _db.Users.AnyAsync(x => x.Name == username, cancellationToken: cancellationToken);
        if (isTaken) return new UsernameTaken();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union6<Success, UsernameTaken, UsernameError, RecentlyChanged, AccountDeactivated, NotFound>> ChangeUsernameAsync(Guid userId, string username, Guid? actorId, bool ignoreLimit = false, CancellationToken cancellationToken = default)
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
        switch (availability.Value)
        {
            case UsernameTaken taken: return taken;
            case UsernameError error: return error;
        }

        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return new NotFound();
        if (user.UserDeactivation is not null) return new AccountDeactivated();
        if (user.Name == username) return new Success(); // Unchanged

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var oldName = user.Name;

        user.Name = username;
        await _db.SaveChangesAsync();

        _db.UserNameChanges.Add(new UserNameChange
        {
            UserId = userId,
            OldName = oldName
        });

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            action: AuditAction.UsernameChanged,
            actorId: actorId ?? userId,
            metadata: new UsernameChangedMetadata(oldName, username)
        );

        await transaction.CommitAsync();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union4<Success, EmailTaken, EmailInvalid, NotFound>>
        ChangeEmail(Guid userId, string email, Guid? actorId, CancellationToken cancellationToken = default)
    {
        // Lookups and uniqueness ignore case in the database. Addresses are still stored lowercased so
        // every account has one canonical form, which the Gravatar hash depends on.
        if (!EmailValidator.TryNormalizeAddress(email, out var normalized))
            return new EmailInvalid();
        email = normalized;

        var availability = await CheckEmailAvailability(email, cancellationToken);
        if (availability is EmailTaken)
            return new EmailTaken();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return new NotFound();

        if (user.Email == email) return new Success(); // Unchanged

        var oldEmail = user.Email;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // An administrator decided whose address this is, so a pending signup parked on it goes. Part of
        // this transaction: it only stays removed if the change is committed.
        await ReleaseEmailFromPendingAccountAsync(email, claimantOwnsEmail: true, exceptUserId: userId, cancellationToken);

        user.Email = email;
        // Outstanding reset and email-change rows carry the stamp they were created under, so rotating
        // it invalidates them here. Without this a pending email-change verification could later
        // overwrite the address an admin just set.
        user.SecurityStamp = Guid.CreateVersion7();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Another account claimed the address between the availability check and this write; the
            // unique index is the real arbiter, so report the conflict the signature declares.
            await transaction.RollbackAsync();
            return new EmailTaken();
        }

        await _auditService.LogAsync(
            userId,
            action: AuditAction.EmailChanged,
            actorId: actorId ?? userId,
            metadata: new EmailChangedMetadata(oldEmail, email)
        );

        await transaction.CommitAsync();

        return new Success();
    }


    /// <inheritdoc />
    public async Task<Union4<Success, AccountNotActivated, AccountDeactivated, NotFound>> ChangePasswordAsync(Guid userId, string newPassword, Guid? actorId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return new NotFound();
        if (user.ActivatedAt is null) return new AccountNotActivated();
        if (user.UserDeactivation is not null) return new AccountDeactivated();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        user.PasswordHash = HashingUtils.HashPassword(newPassword);
        user.SecurityStamp = Guid.CreateVersion7(); // Any outstanding reset/email-change row for this user has a stale SecurityStampAtCreate after this; predicate handles invalidation.

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            action: AuditAction.PasswordChanged,
            actorId: actorId ?? userId
        );

        await transaction.CommitAsync();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<Union7<Success, EmailAlreadyInUse, EmailUnchanged, TooManyEmailChanges, AccountNotActivated, AccountDeactivated, NotFound>> CreateEmailChangeFlowAsync(Guid userId, string newEmail, Guid? actorId, CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(cancellationToken);
        if (data is null) return new NotFound();
        if (data.User.ActivatedAt is null) return new AccountNotActivated();
        if (data.IsDeactivated) return new AccountDeactivated();
        if (string.Equals(data.User.Email, newEmail, StringComparison.OrdinalIgnoreCase)) return new EmailUnchanged();
        if (data.PendingCount >= 3) return new TooManyEmailChanges();

        if (await IsEmailProviderBlacklisted(newEmail, cancellationToken))
            return new EmailAlreadyInUse(); // Don't reveal blacklist hits

        // A never activated account is only a pending signup and does not reserve its address. One that
        // still holds it when the change is verified is released then, see TryVerifyEmailAsync.
        var lowerCaseEmail = newEmail.ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == lowerCaseEmail && x.ActivatedAt != null, cancellationToken))
            return new EmailAlreadyInUse();

        // The verification token is minted lazily by the outbox delivery job at send time, so no usable
        // verification link is ever stored.
        var emailChange = new UserEmailChange
        {
            UserId = data.User.Id,
            OldEmail = data.User.Email,
            NewEmail = lowerCaseEmail,
            TokenHash = SeedTokenHash(),
            SecurityStampAtCreate = data.User.SecurityStamp
        };
        
        await using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            _db.UserEmailChanges.Add(emailChange);

            // Durably enqueue both emails: the verification link to the new address, and an informational
            // notice to the previous address so the legitimate owner sees the change request even if the
            // session/password used to start it was compromised. The outbox guarantees delivery with
            // retries; this replaces the previous best-effort inline sends. Committing the row even if
            // mail later fails is intentional - the request is durable and the delivery job keeps retrying.
            _db.EmailOutbox.Add(EmailOutboxMessage.ForEmailVerification(emailChange.Id, data.User.Id, lowerCaseEmail, data.User.Name));
            _db.EmailOutbox.Add(EmailOutboxMessage.ForEmailChangeNotice(lowerCaseEmail, data.User.Email, data.User.Name));

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                action: AuditAction.EmailChangeRequested,
                actorId: actorId ?? userId,
                metadata: new EmailChangeRequestedMetadata(newEmail)
            );

            await transaction.CommitAsync();
        }
        
        await NotifyEmailOutboxAsync();

        return new Success();
    }

    public async Task<Union3<VerifyEmailSuccess, NotFound, EmailAlreadyInUse>> TryVerifyEmailAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        var hash = HashingUtils.HashToken(token);
        var validSince = DateTime.UtcNow - Duration.EmailChangeRequestLifetime;

        // Scoped to the signed-in user: the link goes to an address that is not the account's yet, so
        // whoever opens it may be a stranger being asked to vouch for someone else's account.
        var change = await _db.UserEmailChanges
            .Where(x => x.UserId == userId && x.TokenHash == hash && x.UsedAt == null && x.CreatedAt >= validSince
                && x.SecurityStampAtCreate == x.User.SecurityStamp
                && x.User.UserDeactivation == null && x.User.ActivatedAt != null)
            .Select(x => new
            {
                ChangeId = x.Id,
                x.UserId,
                // Use the email captured when the change was requested, not the user's current email,
                // which may have changed via another flow between request creation and verification.
                x.OldEmail,
                x.NewEmail,
                x.SecurityStampAtCreate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (change is null) return new NotFound();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Race-safe consume + apply: only updates if SecurityStamp still matches the snapshot, so
        // sibling email changes / password resets that completed since the read above cleanly lose.
        var newStamp = Guid.CreateVersion7();
        try
        {
            // The caller opened the link mailed to the new address while signed in, which proves it is
            // theirs: a pending signup someone parked on it goes. Rolled back with the transaction on every
            // return below that does not commit.
            await ReleaseEmailFromPendingAccountAsync(change.NewEmail, claimantOwnsEmail: true, exceptUserId: change.UserId, cancellationToken);

            var userRows = await _db.Users
                .Where(u => u.Id == change.UserId && u.SecurityStamp == change.SecurityStampAtCreate)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Email, change.NewEmail)
                    .SetProperty(u => u.SecurityStamp, newStamp));
            if (userRows == 0) return new NotFound();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // A bulk update is not wrapped in a DbUpdateException the way SaveChanges wraps it.
            // Another account claimed this email between request creation and verification.
            // The pending row stays as-is (not marked used) so it can expire naturally.
            return new EmailAlreadyInUse();
        }

        var now = DateTime.UtcNow;
        await _db.UserEmailChanges
            .Where(c => c.Id == change.ChangeId && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, now));

        await _auditService.LogAsync(
            change.UserId,
            action: AuditAction.EmailChanged,
            actorId: change.UserId,
            metadata: new EmailChangedMetadata(change.OldEmail, change.NewEmail)
        );

        await transaction.CommitAsync();

        return new VerifyEmailSuccess(change.UserId, change.OldEmail, change.NewEmail);
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
