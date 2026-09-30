namespace OpenShock.API.Services.Turnstile;

public enum CloudflareTurnstileError
{
    MissingSecret,
    InvalidSecret,
    MissingResponse,
    InvalidResponse,
    BadRequest,
    TimeoutOrDuplicate,
    InternalServerError,
}

public static class CloudflareTurnstileErrorExtensions
{
    /// <summary>
    /// Whether the error is caused by the caller's token (missing, malformed, expired, or already redeemed)
    /// rather than by our configuration or a Cloudflare-side fault. Client errors warrant a 4xx response
    /// and should not be logged as server errors.
    /// </summary>
    public static bool IsClientError(this CloudflareTurnstileError error) => error switch
    {
        CloudflareTurnstileError.MissingResponse => true,
        CloudflareTurnstileError.InvalidResponse => true,
        CloudflareTurnstileError.TimeoutOrDuplicate => true,
        _ => false,
    };
}