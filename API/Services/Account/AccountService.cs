using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Email;
using OpenShock.API.Services.Email.Mailjet.Mail;
using OpenShock.API.Utils;
using OpenShock.Common;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using OpenShock.Common.Validation;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Services.Account;

/// <summary>
/// Default implementation of IAccountService
/// </summary>
public sealed class AccountService : IAccountService
{
    private const HashType HashAlgo = HashType.SHA512;

    private readonly OpenShockContext _db;
    private readonly IEmailService _emailService;
    private readonly IRedisCollection<LoginSession> _loginSessions;
    private readonly ILogger<AccountService> _logger;
    private readonly ApiConfig _apiConfig;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="emailService"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="logger"></param>
    /// <param name="apiConfig"></param>
    public AccountService(OpenShockContext db, IEmailService emailService,
        IRedisConnectionProvider redisConnectionProvider, ILogger<AccountService> logger, ApiConfig apiConfig)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _apiConfig = apiConfig;
        _loginSessions = redisConnectionProvider.RedisCollection<LoginSession>(false);
    }

    public TimeSpan SessionLifetime { get; } = TimeSpan.FromDays(30);

    /// <inheritdoc />
    public Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccount(string email, string username,
        string password)
    {
        return CreateAccount(email, username, password, true);
    }

    private async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> CreateAccount(string email,
        string username,
        string password, bool emailActivated)
    {
        if (await _db.Users.AnyAsync(x => x.Email == email.ToLowerInvariant() || x.Name == username))
            return new AccountWithEmailOrUsernameExists();

        var newGuid = Guid.NewGuid();
        var user = new User
        {
            Id = newGuid,
            Name = username,
            Email = email.ToLowerInvariant(),
            PasswordHash = PasswordHashingUtils.HashPassword(password),
            EmailActived = emailActivated
        };
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return new Success<User>(user);
    }

    /// <inheritdoc />
    public async Task<OneOf<Success<User>, AccountWithEmailOrUsernameExists>> Signup(string email, string username,
        string password)
    {
        var accountCreate = await CreateAccount(email, username, password, false);
        if (accountCreate.IsT1) return accountCreate;

        var user = accountCreate.AsT0.Value;

        var id = Guid.NewGuid();
        var secret = CryptoUtils.RandomString(32);
        var secretHash = BCrypt.Net.BCrypt.EnhancedHashPassword(secret, HashAlgo);

        _db.UsersActivations.Add(new UsersActivation()
        {
            Id = id,
            UserId = user.Id,
            Secret = secretHash
        });

        await _db.SaveChangesAsync();

        await _emailService.VerifyEmail(new Contact(email, username),
            new Uri(_apiConfig.Frontend.BaseUrl, $"/#/account/activate/{id}/{secret}"));
        return new Success<User>(user);
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
            await Task.Delay(100,
                cancellationToken); // TODO: Set appropriate time to match password hashing time, preventing timing attacks
            return new NotFound();
        }

        if (!await CheckPassword(password, user)) return new NotFound();

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
    public async Task<OneOf<Success, NotFound, SecretInvalid>> PasswordResetExists(Guid passwordResetId, string secret,
        CancellationToken cancellationToken = default)
    {
        var validUntil = DateTime.UtcNow.Add(Constants.PasswordResetRequestLifetime);
        var reset = await _db.PasswordResets.SingleOrDefaultAsync(x =>
                x.Id == passwordResetId && x.UsedOn == null && x.CreatedOn < validUntil,
            cancellationToken: cancellationToken);

        if (reset == null) return new NotFound();
        if (!BCrypt.Net.BCrypt.EnhancedVerify(secret, reset.Secret, HashAlgo)) return new SecretInvalid();
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, TooManyPasswordResets, NotFound>> CreatePasswordReset(string email)
    {
        var validUntil = DateTime.UtcNow.Add(Constants.PasswordResetRequestLifetime);
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
            new Uri(_apiConfig.Frontend.BaseUrl, $"/#/account/password/recover/{passwordReset.Id}/{secret}"));

        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound, SecretInvalid>> PasswordResetComplete(Guid passwordResetId,
        string secret, string newPassword)
    {
        var validUntil = DateTime.UtcNow.Add(Constants.PasswordResetRequestLifetime);

        var reset = await _db.PasswordResets.Include(x => x.User).SingleOrDefaultAsync(x =>
            x.Id == passwordResetId && x.UsedOn == null && x.CreatedOn < validUntil);

        if (reset == null) return new NotFound();
        if (!BCrypt.Net.BCrypt.EnhancedVerify(secret, reset.Secret, HashAlgo)) return new SecretInvalid();

        reset.UsedOn = DateTime.UtcNow;
        reset.User.PasswordHash = PasswordHashingUtils.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return new Success();
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, UsernameTaken, UsernameError>> CheckUsernameAvailability(string username,
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
    public async Task<OneOf<Success, Error<OneOf<UsernameTaken, UsernameError>>, NotFound>> ChangeUsername(Guid userId,
        string username, bool ignoreLimit = false)
    {
        var availability = await CheckUsernameAvailability(username);
        if (availability.IsT1) return new Error<OneOf<UsernameTaken, UsernameError>>(availability.AsT1);
        if (availability.IsT2) return new Error<OneOf<UsernameTaken, UsernameError>>(availability.AsT2);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return new NotFound();

        user.Name = username;
        await _db.SaveChangesAsync();
        return new Success();
    }


    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound>> ChangePassword(Guid userId, string newPassword)
    {
        var user = await _db.Users.Where(x => x.Id == userId).ExecuteUpdateAsync(calls =>
            calls.SetProperty(x => x.PasswordHash, PasswordHashingUtils.HashPassword(newPassword)));
        return user switch
        {
            <= 0 => new NotFound(),
            1 => new Success(),
            _ => throw new Exception("Updated more than row during password reset"),
        };
    }


    private async Task<bool> CheckPassword(string password, User user)
    {
        var result = PasswordHashingUtils.VerifyPassword(password, user.PasswordHash);

        if (!result.Verified)
        {
            _logger.LogInformation("Failed to verify password for user ID: [{Id}]", user.Id);
            return false;
        }

        if (result.NeedsRehash)
        {
            _logger.LogInformation("Rehashing password for user ID: [{Id}]", user.Id);
            user.PasswordHash = PasswordHashingUtils.HashPassword(password);
            await _db.SaveChangesAsync();
        }

        return true;
    }
}