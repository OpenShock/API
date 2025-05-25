using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public class ReportTokensRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string TurnstileResponse { get; set; }
    public required string[] Secrets { get; set; }
}