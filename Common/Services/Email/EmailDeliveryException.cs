namespace OpenShock.Common.Services.Email;

/// <summary>
/// Thrown by an <see cref="IEmailSender"/> when handing an email to the upstream provider fails.
/// <see cref="IsTransient"/> distinguishes failures that are worth retrying (provider 5xx / 429,
/// network blips) from permanent ones (4xx such as a malformed request or rejected recipient).
/// </summary>
public sealed class EmailDeliveryException : Exception
{
    /// <summary>
    /// True if the failure is expected to be temporary and the send should be retried later.
    /// </summary>
    public bool IsTransient { get; }

    public EmailDeliveryException(bool isTransient, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}
