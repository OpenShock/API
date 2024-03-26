using OpenShock.ServicesCommon.Problems;

namespace OpenShock.ServicesCommon.Errors;

public static class ExceptionError
{
    public static ExceptionProblem Exception => new ExceptionProblem();
}