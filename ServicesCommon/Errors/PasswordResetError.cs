using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.ServicesCommon.Errors;

public static class PasswordResetError
{
    public static OpenShockProblem PasswordResetNotFound => new("PasswordReset.NotFound", "Password reset request not found", HttpStatusCode.NotFound);
}