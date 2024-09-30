using OneOf.Types;
using System.Net;

namespace OpenShock.ServicesCommon.Services.Turnstile;

public interface ICloudflareTurnstileService
{
    /// <summary>
    /// Verify a users turnstile response token
    /// </summary>
    /// <param name="responseToken"></param>
    /// <param name="remoteIpAddress"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Success, No response token was supplied, internal error in cloudflare turnstile, business logic error on turnstile validation</returns>
    public Task<OneOf.OneOf<Success, MissingInput, Error, Error<IReadOnlyList<string>>>> VerifyUserResponseToken(
        string responseToken, IPAddress? remoteIpAddress, CancellationToken cancellationToken = default);
}