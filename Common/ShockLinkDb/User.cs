using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool EmailActived { get; set; }

    public virtual ICollection<DeviceShare> DeviceShares { get; } = new List<DeviceShare>();

    public virtual ICollection<Device> Devices { get; } = new List<Device>();
}
