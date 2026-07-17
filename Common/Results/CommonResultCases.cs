namespace OpenShock.Common.Results;

// Shared case types for use in C# union declarations, replacing the equivalent
// marker types formerly provided by OneOf.Types (OneOf package).

public readonly struct Success;

public readonly struct Success<T>(T value)
{
    public T Value { get; } = value;
}

public readonly struct NotFound;

public readonly struct Error;

public readonly struct Error<T>(T value)
{
    public T Value { get; } = value;
}

public readonly struct None;
