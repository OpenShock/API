using System.Net;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;

namespace OpenShock.API.Services.Token;

/// <summary>
/// Handles persistence and mapping for user API tokens.
/// </summary>
/// <remarks>
/// Read/mutate operations take an optional <c>ownerId</c> restriction. Pass the current user's id
/// for owner-scoped (IDOR-safe) access, or <c>null</c> for unrestricted access (admin/system).
/// The caller (controller) decides which to use.
/// </remarks>
public interface IApiTokenService
{
    /// <summary>
    /// List all tokens owned by <paramref name="ownerId"/>.
    /// </summary>
    IAsyncEnumerable<TokenResponse> ListTokensV1(Guid ownerId);

    /// <inheritdoc cref="ListTokensV1"/>
    IAsyncEnumerable<TokenResponseV2> ListTokensV2(Guid ownerId);

    /// <summary>
    /// Get a single token by id (optionally restricted to an owner), or <c>null</c> if it does not exist.
    /// </summary>
    Task<TokenResponse?> GetTokenV1(Guid tokenId, Guid? ownerId = null);

    /// <inheritdoc cref="GetTokenV1"/>
    Task<TokenResponseV2?> GetTokenV2(Guid tokenId, Guid? ownerId = null);

    /// <summary>
    /// Create a new token owned by <paramref name="userId"/> and return the response including the raw token secret.
    /// </summary>
    Task<TokenCreatedResponse> CreateTokenV1(Guid userId, IPAddress createdByIp, CreateTokenRequest body);

    /// <inheritdoc cref="CreateTokenV1"/>
    Task<TokenCreatedResponseV2> CreateTokenV2(Guid userId, IPAddress createdByIp, CreateTokenRequestV2 body);

    /// <summary>
    /// Edit a token by id (optionally restricted to an owner). Returns <c>false</c> if the token does not exist.
    /// </summary>
    Task<bool> EditTokenV1(Guid tokenId, EditTokenRequest body, Guid? ownerId = null);

    /// <inheritdoc cref="EditTokenV1"/>
    Task<bool> EditTokenV2(Guid tokenId, EditTokenRequestV2 body, Guid? ownerId = null);

    /// <summary>
    /// Set the paused state of a token by id (optionally restricted to an owner).
    /// Returns the now-set paused state, or <c>null</c> if the token does not exist.
    /// </summary>
    Task<bool?> SetTokenPaused(Guid tokenId, bool paused, Guid? ownerId = null);

    /// <summary>
    /// Delete a token by id (optionally restricted to an owner). Returns <c>false</c> if no token was deleted.
    /// </summary>
    Task<bool> DeleteToken(Guid tokenId, Guid? ownerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the minimum info needed to write an audit log entry for a token deletion, or <c>null</c> if the token does not exist.
    /// </summary>
    Task<(Guid OwnerId, string Name)?> GetTokenAuditInfoAsync(Guid tokenId, Guid? ownerId = null, CancellationToken cancellationToken = default);
}