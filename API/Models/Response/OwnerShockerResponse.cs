namespace ShockLink.API.Models.Response;

public class OwnerShockerResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }

    public IList<SharedDevice> Devices { get; set; } = new List<SharedDevice>();

    public class SharedDevice
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }

        public IList<SharedShocker> Shockers { get; set; } = new List<SharedShocker>();

        public class SharedShocker
        {
            public required Guid Id { get; set; }
            public required string Name { get; set; }
            public required bool IsPaused { get; set; }
            public required bool PermSound { get; set; }
            public required bool PermVibrate { get; set; }
            public required bool PermShock { get; set; }
        }
    }
}