using System.Net;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Errors;

public static class PasswordResetError
{
    public static OpenShockProblem PasswordResetNotFound => new("PasswordReset.NotFound", "Password reset request not found", HttpStatusCode.NotFound);
}