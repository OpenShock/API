
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class ChangeEmailRequest
{
    [EmailAddress(true)]
    public required string Email { get; set; }
}