namespace OpenShock.Common.Models;

public sealed class Paginated<T>
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public long Total { get; set; }
    public IEnumerable<T> Data { get; set; }
}