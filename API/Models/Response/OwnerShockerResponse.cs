namespace OpenShock.API.Models.Response;

public sealed class OwnerShockerResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Uri Image { get; init; }
    public required SharedDevice[] Devices { get; init; }

    public sealed class SharedDevice
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        // ReSharper disable once CollectionNeverQueried.Global
        public required SharedShocker[] Shockers { get; init; }

        public sealed class SharedShocker
        {
            public required Guid Id { get; init; }
            public required string Name { get; init; }
            public required bool IsPaused { get; init; }
            public required ShockerPermissions Permissions { get; init; }
            public required ShockerLimits Limits { get; init; }
        }
    }
}