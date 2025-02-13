using OpenShock.Serialization.Types;
using Semver;

namespace OpenShock.Common.Extensions;

public static class SemVersionExtensions
{
    public static SemVer ToSemVer(this SemVersion version) => new()
    {
        Major = (ushort)version.Major,
        Minor = (ushort)version.Minor,
        Patch = (ushort)version.Patch,
        Prerelease = version.Prerelease,
        Build = version.Metadata
    };
    
    public static SemVersion ToSemVersion(this SemVer version) => 
        SemVersion.ParsedFrom(version.Major, version.Minor, version.Patch, version.Prerelease ?? string.Empty, version.Build ?? string.Empty);
}