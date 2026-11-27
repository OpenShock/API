using OpenShock.Common.Utils;

namespace OpenShock.Common.Extensions;

public static class EnvironmentExtensions
{
    extension(Environment)
    {
        /// <summary>
        /// Whether the environment variable <paramref name="variable"/> is set to a truthy value
        /// (<c>1</c>, <c>true</c> or <c>yes</c>, see <see cref="StringUtils.IsTruthy"/>). Unset counts as false.
        /// </summary>
        public static bool IsFlagSet(string variable)
            => StringUtils.IsTruthy(Environment.GetEnvironmentVariable(variable));
    }
}
