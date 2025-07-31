﻿using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;

namespace OpenShock.Common;

/// <summary>
/// Custom redis backplane for SignalR
/// Use #local in front of user id's to target only locally connected clients.
/// </summary>
/// <typeparam name="THub"></typeparam>
public sealed class OpenShockRedisHubLifetimeManager<THub> : RedisHubLifetimeManager<THub> where THub : Hub
{
    private readonly FieldInfo _usersField;
    private readonly FieldInfo _subscriptionsField;
    private readonly string _redisPrefix = typeof(THub).FullName!;
    
    /// <summary>
    /// Constructs the <see cref="OpenShockRedisHubLifetimeManager{THub}"/> with types from Dependency Injection.
    /// </summary>
    /// <param name="logger">The logger to write information about what the class is doing.</param>
    /// <param name="options">The <see cref="RedisOptions"/> that influence behavior of the Redis connection.</param>
    /// <param name="hubProtocolResolver">The <see cref="IHubProtocolResolver"/> to get an <see cref="IHubProtocol"/> instance when writing to connections.</param>
    /// <param name="globalHubOptions">The global <see cref="HubOptions"/>.</param>
    /// <param name="hubOptions">The <typeparamref name="THub"/> specific options.</param>
    public OpenShockRedisHubLifetimeManager(ILogger<OpenShockRedisHubLifetimeManager<THub>> logger,
        IOptions<RedisOptions> options, IHubProtocolResolver hubProtocolResolver,
        IOptions<HubOptions>? globalHubOptions,
        IOptions<HubOptions<THub>>? hubOptions) : base(logger, options, hubProtocolResolver, globalHubOptions,
        hubOptions)
    {
        _usersField = typeof(RedisHubLifetimeManager<THub>).GetField("_users", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _subscriptionsField = _usersField.FieldType.GetField("_subscriptions", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    /// <inheritdoc />
    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count <= 0) return Task.CompletedTask;

        var localUsers = new List<string>(userIds.Count);
        var remoteUsers = new List<string>(userIds.Count);

        foreach (var userId in userIds)
            if (userId.StartsWith("local#")) localUsers.Add(userId);
            else remoteUsers.Add(userId);

        var tasks = new List<Task>(1 + localUsers.Count);

        if (remoteUsers.Count > 0)
        {
            tasks.Add(base.SendUsersAsync(remoteUsers, methodName, args, cancellationToken));
        }

        if (localUsers.Count > 0)
        {
            var message = new SerializedHubMessage(new InvocationMessage(methodName, args));

            foreach (var userId in localUsers)
            {
                tasks.Add(SendLocalUserAsync(userId, message, cancellationToken));
            }
        }

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public override Task SendUserAsync(string userId, string methodName, object?[] args,
        CancellationToken cancellationToken = default)
    {
        if (userId.StartsWith("local#"))
        {
            var message = new SerializedHubMessage(new InvocationMessage(methodName, args));
            return SendLocalUserAsync(userId, message, cancellationToken);
        }

        return base.SendUserAsync(userId, methodName, args, cancellationToken);
    }

    /// <summary>
    /// Composes a new hub message and sends it to the locally connected user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private Task SendLocalUserAsync(string userId, SerializedHubMessage message,
        CancellationToken cancellationToken)
    {
        var users = _usersField.GetValue(this)!;
        var subsObj =
            (ConcurrentDictionary<string, HubConnectionStore>)_subscriptionsField.GetValue(users)!;

        if (!subsObj.TryGetValue($"{_redisPrefix}:user:{userId[6..]}", out var store)) return Task.CompletedTask;

        var tasks = new List<Task>(store.Count);
        foreach (var context in store) tasks.Add(context.WriteAsync(message, cancellationToken).AsTask());
        return Task.WhenAll(tasks);
    }
}