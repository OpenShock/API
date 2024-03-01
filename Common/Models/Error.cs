namespace OpenShock.Common.Models;

public sealed class Error<TError> where TError : Enum
{
    public required TError Type { get; set; }
}