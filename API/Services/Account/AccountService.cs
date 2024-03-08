using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Services.Account;

/// <summary>
/// Default implementation of IAccountService
/// </summary>
public sealed class AccountService : IAccountService
{
    private const HashType HashAlgo = HashType.SHA512;
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromDays(7);

    private readonly OpenShockContext _db;
    private readonly IEmailService _emailService;
    private readonly IRedisCollection<LoginSession> _loginSessions;
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="emailService"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="logger"></param>
    public AccountService(OpenShockContext db, IEmailService emailService,
        IRedisConnectionProvider redisConnectionProvider, ILogger<AccountService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _loginSessions = redisConnectionProvider.RedisCollection<LoginSession>(false);
    }

    public TimeSpan SessionLifetime { get; } = TimeSpan.FromDays(30);

    /// <inheritdoc />
    public Task<OneOf<Success, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username,
        string password)
    {
        return CreateAccount(email, username, password, true);
    }

    private async Task<OneOf<Success, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username,
        string password, bool emailActivated)
    {
        if(await _db.Users.AnyAsync(x => x.Email == email.ToLowerInvariant() || x.Name == username)) return new AccountWithEmailOrUsernameExists();
        
        var newGuid = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = newGuid,
            Name = username,
            Email = email.ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashAlgo),
            PasswordEncryption = PasswordEncryptionType.BcryptEnhanced,
            EmailActived = emailActivated
        });

        await _db.SaveChangesAsync();

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, AccountWithEmailOrUsernameExists>> Signup(string email, string username,
        string password)
    {
        var accountCreate = await CreateAccount(email, username, password, false);
        if (accountCreate.IsT1) return accountCreate;

        // TODO: Probably make a account activation table? And have a unique id there with an encrypted secret so no one can grab it from the db

        await _emailService.ActivateAccountEmail(new Contact(email, username),
            new Uri(APIGlobals.ApiConfig.FrontendBaseUrl, $"/#/account/activate/{Guid.NewGuid()}"));
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success<string>, NotFound>> Login(string emailOrUsername, string password,
        LoginContext loginContext, CancellationToken cancellationToken = default)
    {
        var lowercaseEmailOrUsername = emailOrUsername.ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(
            x => x.Email == lowercaseEmailOrUsername || x.Name == lowercaseEmailOrUsername,
            cancellationToken: cancellationToken);
        if (user == null)
        {
            _logger.LogInformation("Failed to find user with email or username [{EmailOrUsername}]", emailOrUsername);
            return new NotFound();
        }

        if (!await CheckPassword(emailOrUsername, password, user)) return new NotFound();

        var randomSessionId = CryptoUtils.RandomString(64);

        await _loginSessions.InsertAsync(new LoginSession
        {
            Id = randomSessionId,
            UserId = user.Id,
            UserAgent = loginContext.UserAgent,
            Ip = loginContext.Ip
        }, SessionLifetime);

        return new Success<string>(randomSessionId);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, SecretInvalid>> PasswordResetExists(Guid passwordResetId, string secret, CancellationToken cancellationToken = default)
    {
        var validUntil = DateTime.UtcNow.Add(PasswordResetLifetime);
        var reset = await _db.PasswordResets.SingleOrDefaultAsync(x =>
            x.Id == passwordResetId && x.UsedOn == null && x.CreatedOn < validUntil, cancellationToken: cancellationToken);

        if (reset == null) return new NotFound();
        if(!BCrypt.Net.BCrypt.EnhancedVerify(secret, reset.Secret, HashAlgo)) return new SecretInvalid();
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, TooManyPasswordResets, NotFound>> CreatePasswordReset(string email)
    {
        var validUntil = DateTime.UtcNow.Add(PasswordResetLifetime);
        var lowerCaseEmail = email.ToLowerInvariant();
        var user = await _db.Users.Where(x => x.Email == lowerCaseEmail).Select(x => new
        {
            User = x,
            PasswordResetCount = x.PasswordResets.Count(y => y.UsedOn == null && y.CreatedOn < validUntil)
        }).FirstOrDefaultAsync();
        if (user == null) return new NotFound();
        if (user.PasswordResetCount >= 3) return new TooManyPasswordResets();
        
        var secret = CryptoUtils.RandomString(32);
        var hash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret, HashAlgo);
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            Secret = hash,
            User = user.User
        };
        _db.PasswordResets.Add(passwordReset);
        await _db.SaveChangesAsync();

        await _emailService.PasswordReset(new Contact(user.User.Email, user.User.Name),
            new Uri(APIGlobals.ApiConfig.FrontendBaseUrl, $"/#/account/password/recover/{passwordReset.Id}/{secret}"));
        
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, SecretInvalid>> PasswordResetComplete(Guid passwordResetId, string secret, string newPassword)
    {
        var validUntil = DateTime.UtcNow.Add(PasswordResetLifetime);
        
        var reset = await _db.PasswordResets.Include(x => x.User).SingleOrDefaultAsync(x =>
            x.Id == passwordResetId && x.UsedOn == null && x.CreatedOn < validUntil);

        if (reset == null) return new NotFound();
        if(!BCrypt.Net.BCrypt.EnhancedVerify(secret, reset.Secret, HashAlgo)) return new SecretInvalid();

        reset.UsedOn = DateTime.UtcNow;
        reset.User.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, HashAlgo);
        await _db.SaveChangesAsync();
        return new Success();
    }


    private async Task<bool> CheckPassword(string emailOrUsername, string password, User user)
    {
        // LEGACY PBKDF2
        if (user.PasswordEncryption == PasswordEncryptionType.Pbkdf2)
        {
            if (!SecurePasswordHasher.Verify(password, user.Password))
            {
                _logger.LogInformation("Failed verify hash PBKDF2, EmailOrUsername: [{EmailOrUsername}]",
                    emailOrUsername);

                return false;
            }

            // Upgrade encryption
            var newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashAlgo);
            user.PasswordEncryption = PasswordEncryptionType.BcryptEnhanced;
            user.Password = newHash;
            await _db.SaveChangesAsync();
            return true;
        }

        if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.Password, HashAlgo))
        {
            _logger.LogInformation("Failed to verify BCrypt hash, EmailOrUsername [{EmailOrUsername}]",
                emailOrUsername);
        }


        return true;
    }
}