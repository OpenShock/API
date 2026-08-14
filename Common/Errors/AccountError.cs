using System.Net;
using OpenShock.Common.Problems;
using OpenShock.Common.Validation;

using OpenShock.Internal.Common.Problems;

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
    
    public static OpenShockProblem UsernameRecentlyChanged => new OpenShockProblem(
        "Account.Username.RecentlyChanged", "You have recently changed your username. You can only change your username every 7 days", HttpStatusCode.Forbidden);

    public static OpenShockProblem AccountNotActivated => new OpenShockProblem("Account.AccountNotActivated", "Your account has not been activated", HttpStatusCode.Unauthorized);
    public static OpenShockProblem AccountActivationNotFound => new OpenShockProblem("Account.Activation.NotFound", "There is no account activation request matching the supplied token", HttpStatusCode.BadRequest);
    public static OpenShockProblem AccountDeactivated => new OpenShockProblem("Account.Deactivated", "Your account has been deactivated", HttpStatusCode.Unauthorized);

    public static OpenShockProblem AccountOAuthOnly => new OpenShockProblem("Account.OAuthOnly", "This account is only accessible via OAuth", HttpStatusCode.Unauthorized);

    public static OpenShockProblem PasswordNotSet => new OpenShockProblem("Account.Password.NotSet",
        "This account has no password set. Initiate a password set flow via POST /password/set instead.", HttpStatusCode.Conflict);

    public static OpenShockProblem EmailChangeAlreadyInUse => new OpenShockProblem("Account.Email.AlreadyInUse",
        "This email address is already in use", HttpStatusCode.Conflict);

    public static OpenShockProblem EmailChangeUnchanged => new OpenShockProblem("Account.Email.Unchanged",
        "The new email address is the same as the current one", HttpStatusCode.BadRequest);

    public static OpenShockProblem EmailChangeTooMany => new OpenShockProblem("Account.Email.TooManyRequests",
        "You have too many pending email change requests", HttpStatusCode.TooManyRequests);

    public static OpenShockProblem EmailChangeNotFound => new OpenShockProblem("Account.Email.VerifyNotFound",
        "There is no email change request matching the supplied token", HttpStatusCode.BadRequest);
}