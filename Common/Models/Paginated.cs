namespace OpenShock.Common.Models;

public sealed class Paginated<T>
{
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required long Total { get; init; }
    public required IReadOnlyList<T> Data { get; init; }
}