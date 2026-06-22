using System.Linq.Expressions;
using System.Net;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;

namespace OpenShock.API.Services.Token;

/// <summary>
/// Default implementation of <see cref="IApiTokenService"/>.
/// </summary>
public sealed class ApiTokenService : IApiTokenService
{
    private readonly OpenShockContext _db;
    private readonly IRedisPubService _redisPubService;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="redisPubService"></param>
    public ApiTokenService(OpenShockContext db, IRedisPubService redisPubService)
    {
        _db = db;
        _redisPubService = redisPubService;
    }

    private static readonly Expression<Func<ApiToken, TokenResponse>> ToTokenResponse = x => new TokenResponse
    {
        CreatedOn = x.CreatedAt,
        ValidUntil = x.ValidUntil,
        LastUsed = x.LastUsed ?? default,
        Permissions = x.Permissions,
        Name = x.Name,
        Id = x.Id
    };

    private static readonly Expression<Func<ApiToken, TokenResponseV2>> ToTokenResponseV2 = x => new TokenResponseV2
    {
        CreatedOn = x.CreatedAt,
        ValidUntil = x.ValidUntil,
        LastUsed = x.LastUsed,
        Permissions = x.Permissions,
        Name = x.Name,
        Id = x.Id,
        ShockerControl = new ShockerControlSettings
        {
            Paused = x.ShockerControlPaused,
            Intensity = new IntensityLimitSettings
            {
                Min = x.ShockerControlIntensityMin,
                Max = x.ShockerControlIntensityMax,
                Mode = x.ShockerControlIntensityMode
            },
            Duration = new DurationLimitSettings
            {
                Min = x.ShockerControlDurationMin,
                Max = x.ShockerControlDurationMax,
                Mode = x.ShockerControlDurationMode
            }
        }
    };

    /// <summary>
    /// The set of live (non-expired) tokens, optionally restricted to a single owner.
    /// Expired tokens are excluded everywhere - they can no longer authenticate and are
    /// treated as deleted. Callers pass the current user's id for owner-scoped (IDOR-safe) access,
    /// or <c>null</c> for unrestricted access (admin/system).
    /// </summary>
    private IQueryable<ApiToken> Tokens(Guid? ownerId)
    {
        // Mirror the expiry check in ApiTokenAuthentication (valid while ValidUntil >= now) so a token
        // is hidden here exactly when it can no longer authenticate.
        var query = _db.ApiTokens.Where(x => x.ValidUntil == null || x.ValidUntil >= DateTime.UtcNow);
        return ownerId is { } owner ? query.Where(x => x.UserId == owner) : query;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TokenResponse> ListTokensV1(Guid ownerId) => Tokens(ownerId)
        .OrderBy(x => x.CreatedAt)
        .Select(ToTokenResponse)
        .AsAsyncEnumerable();

    /// <inheritdoc />
    public IAsyncEnumerable<TokenResponseV2> ListTokensV2(Guid ownerId) => Tokens(ownerId)
        .OrderBy(x => x.CreatedAt)
        .Select(ToTokenResponseV2)
        .AsAsyncEnumerable();

    /// <inheritdoc />
    public Task<TokenResponse?> GetTokenV1(Guid tokenId, Guid? ownerId = null) => Tokens(ownerId)
        .Where(x => x.Id == tokenId)
        .Select(ToTokenResponse)
        .FirstOrDefaultAsync();

    /// <inheritdoc />
    public Task<TokenResponseV2?> GetTokenV2(Guid tokenId, Guid? ownerId = null) => Tokens(ownerId)
        .Where(x => x.Id == tokenId)
        .Select(ToTokenResponseV2)
        .FirstOrDefaultAsync();

    /// <inheritdoc />
    public async Task<TokenCreatedResponse> CreateTokenV1(Guid userId, IPAddress createdByIp, CreateTokenRequest body)
    {
        var secret = CryptoUtils.RandomAlphaNumericString(AuthConstants.ApiTokenLength);

        var tokenDto = new ApiToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = body.Name,
            TokenHash = HashingUtils.HashToken(secret),
            CreatedByIp = createdByIp,
            Permissions = body.Permissions.Distinct().ToList(),
            ValidUntil = body.ValidUntil?.ToUniversalTime()
        };
        _db.ApiTokens.Add(tokenDto);
        await _db.SaveChangesAsync();

        return new TokenCreatedResponse
        {
            Id = tokenDto.Id,
            Name = tokenDto.Name,
            Token = secret,
            CreatedAt = tokenDto.CreatedAt,
            ValidUntil = tokenDto.ValidUntil,
            LastUsed = tokenDto.LastUsed ?? default,
            Permissions = tokenDto.Permissions
        };
    }

    /// <inheritdoc />
    public async Task<TokenCreatedResponseV2> CreateTokenV2(Guid userId, IPAddress createdByIp, CreateTokenRequestV2 body)
    {
        var secret = CryptoUtils.RandomAlphaNumericString(AuthConstants.ApiTokenLength);

        var tokenDto = new ApiToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = body.Name,
            TokenHash = HashingUtils.HashToken(secret),
            CreatedByIp = createdByIp,
            Permissions = body.Permissions.Distinct().ToList(),
            ValidUntil = body.ValidUntil?.ToUniversalTime()
        };
        body.ShockerControl.ApplyTo(tokenDto);

        _db.ApiTokens.Add(tokenDto);
        await _db.SaveChangesAsync();

        return new TokenCreatedResponseV2
        {
            Id = tokenDto.Id,
            Name = tokenDto.Name,
            Token = secret,
            CreatedAt = tokenDto.CreatedAt,
            ValidUntil = tokenDto.ValidUntil,
            LastUsed = tokenDto.LastUsed,
            Permissions = tokenDto.Permissions,
            ShockerControl = ShockerControlSettings.FromToken(tokenDto)
        };
    }

    /// <inheritdoc />
    public async Task<bool> EditTokenV1(Guid tokenId, EditTokenRequest body, Guid? ownerId = null)
    {
        var token = await Tokens(ownerId).FirstOrDefaultAsync(x => x.Id == tokenId);
        if (token is null) return false;

        token.Name = body.Name;
        token.Permissions = body.Permissions.Distinct().ToList();
        await _db.SaveChangesAsync();

        await _redisPubService.SendApiTokenUpdate(tokenId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> EditTokenV2(Guid tokenId, EditTokenRequestV2 body, Guid? ownerId = null)
    {
        var token = await Tokens(ownerId).FirstOrDefaultAsync(x => x.Id == tokenId);
        if (token is null) return false;

        token.Name = body.Name;
        token.Permissions = body.Permissions.Distinct().ToList();
        body.ShockerControl.ApplyTo(token);
        await _db.SaveChangesAsync();

        await _redisPubService.SendApiTokenUpdate(tokenId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool?> SetTokenPaused(Guid tokenId, bool paused, Guid? ownerId = null)
    {
        var nUpdated = await Tokens(ownerId)
            .Where(x => x.Id == tokenId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ShockerControlPaused, paused));
        if (nUpdated <= 0) return null;

        await _redisPubService.SendApiTokenUpdate(tokenId);

        return paused;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteToken(Guid tokenId, Guid? ownerId = null, CancellationToken cancellationToken = default)
    {
        var nDeleted = await Tokens(ownerId).Where(x => x.Id == tokenId).ExecuteDeleteAsync(cancellationToken);
        if (nDeleted <= 0) return false;

        // Revoked tokens must stop controlling any open live control connection.
        await _redisPubService.SendApiTokenUpdate(tokenId);

        return true;
    }
}
