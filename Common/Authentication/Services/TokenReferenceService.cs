namespace OpenShock.Common.Authentication.Services;

public sealed class TokenReferenceService<T> : ITokenReferenceService<T> where T : class
{
    public T? Token { get; set; } = null;
}