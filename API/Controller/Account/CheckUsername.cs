using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.ServicesCommon.Problems;
using OpenShock.ServicesCommon.Validation;

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
    [ProducesSuccess<UsernameCheckResponse>]
    public async Task<IActionResult> CheckUsername(ChangeUsernameRequest data, CancellationToken cancellationToken)
    {
        var availability = await _accountService.CheckUsernameAvailability(data.Username, cancellationToken);
        return availability.Match(success => RespondSuccess(new UsernameCheckResponse(UsernameAvailability.Available)),
            taken => RespondSuccess(new UsernameCheckResponse(UsernameAvailability.Taken)),
            invalid => RespondSuccess(new UsernameCheckResponse(UsernameAvailability.Invalid, invalid)));
    }
}

public enum UsernameAvailability
{
    Available,
    Taken,
    Invalid
}

public class UsernameCheckResponse
{
    public required UsernameAvailability Availability { get; init; }
    public UsernameError? Error { get; init; } = null;
    
    [SetsRequiredMembers]
    public UsernameCheckResponse(UsernameAvailability availability, UsernameError? error = null)
    {
        Availability = availability;
        Error = error;
    }
    
    [SetsRequiredMembers]
    public UsernameCheckResponse(UsernameAvailability availability)
    {
        Availability = availability;
    }
}