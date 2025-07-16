using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Validation;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Check if a username is available
    /// </summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("username/check")]
    public async Task<UsernameCheckResponse> CheckUsername(ChangeUsernameRequest data, CancellationToken cancellationToken)
    {
        var result = await _accountService.CheckUsernameAvailability(data.Username, cancellationToken);

        return result.Match(
            success => new UsernameCheckResponse(UsernameAvailability.Available),
            taken => new UsernameCheckResponse(UsernameAvailability.Taken),
            invalid => new UsernameCheckResponse(UsernameAvailability.Invalid, invalid)
        );
    }
}

public enum UsernameAvailability
{
    Available,
    Taken,
    Invalid
}

public sealed class UsernameCheckResponse
{
    [SetsRequiredMembers]
    public UsernameCheckResponse(UsernameAvailability availability, UsernameError? error = null)
    {
        Availability = availability;
        Error = error;
    }
    
    public required UsernameAvailability Availability { get; init; }
    public UsernameError? Error { get; init; }
}