using System.Reflection;

namespace OpenShock.API.Utils;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GitHashAttribute : Attribute
{
    public string Hash { get; }
    public GitHashAttribute(string hsh)
    {
        Hash = hsh;
    }

    public static string FullHash = Assembly.GetEntryAssembly()?.GetCustomAttribute<GitHashAttribute>()?.Hash ?? "error";
}
