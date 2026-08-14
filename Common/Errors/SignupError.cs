using System.Net;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common.Errors;

public static class SignupError
{
    public static OpenShockProblem UsernameOrEmailExists => new(
        "Signup.UsernameOrEmailExists",
        "The chosen username or email is already in use",
        HttpStatusCode.Conflict);

    public static OpenShockProblem RegistrationDisabled => new(
        "Signup.RegistrationDisabled",
        "New account registration is currently disabled",
        HttpStatusCode.Forbidden);
}