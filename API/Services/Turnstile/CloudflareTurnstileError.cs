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