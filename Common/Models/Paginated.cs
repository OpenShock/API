namespace OpenShock.Common.Models;

public sealed class Paginated<T>
{
    public required int Offset { get; set; }
    public required int Limit { get; set; }
    public required long Total { get; set; }
    public required IReadOnlyList<T> Data { get; set; }
}