using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenShock.Common.Models;

namespace OpenShock.Common.DataAnnotations;

public static class OpenApiSchemas
{
    public static OpenApiSchema SemVerSchema => new OpenApiSchema {
        Title = "SemVer",
        Type = "string",
        Pattern = /* lang=regex */ "^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$",
        Example = new OpenApiString("1.0.0-dev+a16f2")
    };

    public static OpenApiSchema PauseReasonEnumSchema => new OpenApiSchema {
        Title = nameof(PauseReason),
        Type = "integer",
        Description = """
            An integer representing the reason(s) for the shocker being paused, expressed as a bitfield where reasons are OR'd together.

            Each bit corresponds to:
            - 1: Shocker
            - 2: Share
            - 4: ShareLink

            For example, a value of 6 (2 | 4) indicates both 'Share' and 'ShareLink' reasons.
            """,
        Example = new OpenApiInteger(6)
    };
}
