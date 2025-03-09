﻿using System.Net;
using OpenShock.Common.Models;

namespace OpenShock.Common.Problems.CustomProblems;

public sealed class TokenPermissionProblem(
    string type,
    string title,
    PermissionType requiredPermission,
    IEnumerable<PermissionType> grantedPermissions,
    HttpStatusCode status = HttpStatusCode.BadRequest,
    string? detail = null)
    : OpenShockProblem(type, title, status, detail)
{
    public PermissionType RequiredPermission { get; set; } = requiredPermission;
    public IEnumerable<PermissionType> GrantedPermissions { get; set; } = grantedPermissions;
}