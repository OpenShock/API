using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ChangeUsernameRequest
{
    [Username(true)]
    public required string Username { get; init; }
}