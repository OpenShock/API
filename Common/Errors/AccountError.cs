using System.Net;
using OpenShock.Common.Problems;
using OpenShock.Common.Validation;

namespace OpenShock.Common.Errors;

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

    public static OpenShockProblem PasswordChangeInvalidPassword => new OpenShockProblem(
        "Account.Password.OldPasswordInvalid", "The old password is invalid", HttpStatusCode.Forbidden);
}