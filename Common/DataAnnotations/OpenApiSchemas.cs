using Microsoft.OpenApi;
using OpenShock.Common.Models;

namespace OpenShock.Common.DataAnnotations;

public static class OpenApiSchemas
{
    public static OpenApiSchema SemVerSchema => new()
    {
        Title = "SemVer",
        Type = JsonSchemaType.String,
        Pattern = /* lang=regex */ @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        Example = "1.0.0-dev+a16f2"
    };

    public static OpenApiSchema PauseReasonEnumSchema => new()
    {
        Title = nameof(PauseReason),
        Type = JsonSchemaType.Integer,
        Description = """
            An integer representing the reason(s) for the shocker being paused, expressed as a bitfield where reasons are OR'd together.

            Each bit corresponds to:
            - 1: Shocker
            - 2: UserShare
            - 4: PublicShare

            For example, a value of 6 (2 | 4) indicates both 'UserShare' and 'PublicShare' reasons.
            """,
        Example = 6
    };
}
