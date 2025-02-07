using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class AdminError
{
    public static OpenShockProblem CannotDeletePrivledgedAccount => new OpenShockProblem("User.Privileged.DeleteDenied",
        "You cannot delete a privileged user", HttpStatusCode.Forbidden);
    
    public static OpenShockProblem UserNotFound => new("User.NotFound", "User not found", HttpStatusCode.NotFound);

    public static OpenShockProblem EmailTaken => new OpenShockProblem("Account.Email.Taken",
        "This email is already in use", HttpStatusCode.Conflict);
    public static OpenShockProblem UsernameTaken => new OpenShockProblem("Account.Username.Taken",
        "This username is already in use", HttpStatusCode.Conflict);

    public static OpenShockProblem EmailInvalid => new OpenShockProblem("Account.Email.Invalid",
        "This email is not valid", HttpStatusCode.BadRequest);
}