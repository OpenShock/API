﻿namespace OpenShock.ServicesCommon.Authentication.Services;

public sealed class ClientAuthService<T> : IClientAuthService<T> where T : class
{
    public T CurrentClient { get; set; } = null!;
}