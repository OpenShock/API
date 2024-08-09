using System.Net;
using OpenShock.ServicesCommon.Problems;
using OpenShock.ServicesCommon.Validation;

namespace OpenShock.ServicesCommon.Errors;

public static class AccountError
{
    public static OpenShockProblem UsernameTaken => new OpenShockProblem("Account.Username.Taken",
        "This username is already in use", HttpStatusCode.Conflict);

    public static OpenShockProblem UsernameInvalid(UsernameError usernameError) => new OpenShockProblem(
        "Account.Username.Invalid",
        "This username is invalid", HttpStatusCode.BadRequest)
    {
        Extensions = new Dictionary<string, object?>
        {
            { "usernameError", usernameError }
        }
    };
}