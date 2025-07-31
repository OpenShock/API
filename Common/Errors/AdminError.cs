using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class AdminError
{
    public static OpenShockProblem CannotDeletePrivledgedAccount => new OpenShockProblem("User.Privileged.DeleteDenied",
        "You cannot delete a privileged user", HttpStatusCode.Forbidden);
    
    public static OpenShockProblem UserNotFound => new("User.NotFound", "User not found", HttpStatusCode.NotFound);
    
    public static OpenShockProblem WebhookNotFound => new OpenShockProblem("Webhook.NotFound", "Webhook not found", HttpStatusCode.NotFound);
    public static OpenShockProblem WebhookOnlyDiscord => new OpenShockProblem("Webhook.Unsupported", "Only discord webhooks work as of now! Make sure to use discord.com as the host, and not canary or ptb.", HttpStatusCode.BadRequest);
}