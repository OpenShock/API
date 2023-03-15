using System;
using System.Collections.Generic;
using ShockLink.Common.Models;

namespace ShockLink.Common.ShockLinkDb;

public partial class CfImage
{
    public Guid Id { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }
    
    public CfImageType Type { get; set; }

    public virtual ICollection<User> Users { get; } = new List<User>();
}
