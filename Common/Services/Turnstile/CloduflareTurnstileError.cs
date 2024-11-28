namespace OpenShock.Common.Services.Turnstile;

public enum CloduflareTurnstileError
{
    MissingSecret,
    InvalidSecret,
    MissingResponse,
    InvalidResponse,
    BadRequest,
    TimeoutOrDuplicate,
    InternalServerError,
}