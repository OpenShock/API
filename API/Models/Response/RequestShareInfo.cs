﻿using OpenShock.API.Models.Requests;
using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class RequestShareInfo : ShockerPermLimitPairWithId
{
    public required GenericIni? SharedWith { get; set; }
    public required DateTime CreatedOn { get; set; }
}