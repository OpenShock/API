using System.Net;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

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

    // The target account's state blocks the operation - the admin is authorised, so these are 409
    // rather than the 401s the equivalent self-service AccountError entries return.
    public static OpenShockProblem UserNotActivated => new OpenShockProblem("Admin.User.NotActivated",
        "This user's account has not been activated yet", HttpStatusCode.Conflict);
    public static OpenShockProblem UserDeactivated => new OpenShockProblem("Admin.User.Deactivated",
        "This user's account is deactivated", HttpStatusCode.Conflict);
    
    public static OpenShockProblem WebhookNotFound => new OpenShockProblem("Webhook.NotFound", "Webhook not found", HttpStatusCode.NotFound);
    public static OpenShockProblem WebhookOnlyDiscord => new OpenShockProblem("Webhook.Unsupported", "Only discord webhooks work as of now! Make sure to use discord.com as the host, and not canary or ptb.", HttpStatusCode.BadRequest);
}