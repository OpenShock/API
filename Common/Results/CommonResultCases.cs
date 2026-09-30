namespace OpenShock.Common.Results;

// Shared case types for use in C# union declarations, replacing the equivalent
// marker types formerly provided by OneOf.Types (OneOf package).

public sealed class Success;

public sealed class Success<T>(T value)
{
    public T Value { get; } = value;
}

public sealed class NotFound;

public sealed class NotFound<T>(T value)
{
    public T Value { get; } = value;
}

public sealed class Error;

public sealed class None;

/// <summary>
/// When the websocket sent a close frame
/// </summary>
public sealed class WebsocketClosure;