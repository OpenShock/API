using System.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Turnstile;
using OpenShock.Common.Utils;
using System.Net;
using OpenShock.Common.Services.Webhook;

namespace OpenShock.API.Controller.Tokens;

public sealed partial class TokensController
{
    /// <summary>
    /// Endpoint to delete potentially compromised api tokens
    /// </summary>
    /// <param name="body"></param>
    /// <param name="turnstileService"></param>
    /// <param name="webhookService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">The tokens were deleted if found</response>
    [HttpPost("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReportTokens(
        [FromBody] ReportTokensRequest body,
        [FromServices] ICloudflareTurnstileService turnstileService,
        [FromServices] IWebhookService webhookService,
        CancellationToken cancellationToken)
    {
        var remoteIP = HttpContext.GetRemoteIP();

        var turnStile = await turnstileService.VerifyUserResponseToken(body.TurnstileResponse, remoteIP, cancellationToken);
        if (!turnStile.IsT0)
        {
            var cfErrors = turnStile.AsT1.Value!;
            if (cfErrors.All(err => err == CloduflareTurnstileError.InvalidResponse))
                return Problem(TurnstileError.InvalidTurnstile);

            return Problem(new OpenShockProblem("InternalServerError", "Internal Server Error", HttpStatusCode.InternalServerError));
        }

        var reportId = Guid.CreateVersion7();

        int nAffected = 0;
        try
        {
            var hashes = new string[body.Secrets.Length];
            for (int i = 0; i < body.Secrets.Length; i++)
            {
                body.Secrets[i] = HashingUtils.HashSha256(body.Secrets[i]);
            }
            
            nAffected = await _db.ApiTokens
                .Where(x => hashes.Contains(x.TokenHash))
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Failed to delete reported api tokens.");
        }

        var ipCountry = HttpContext.GetCFIPCountry();
        
        _db.ApiTokenReports.Add(new Common.OpenShockDb.ApiTokenReport {
            Id = reportId,
            SubmittedCount = body.Secrets.Length,
            AffectedCount = nAffected,
            UserId = CurrentUser.Id,
            IpAddress = remoteIP,
            IpCountry = ipCountry,
            ReportedByUser = CurrentUser
        });
        await _db.SaveChangesAsync(cancellationToken);

        await webhookService.SendWebhook(
            "TokensReported",
            "ApiToken leak report submitted", 
        $"""
               🔒 **API Token Report**
               
               A new API token leak report has been submitted by **{CurrentUser.Name}** (`{CurrentUser.Id}`).
               
               • 📄 **Submitted Tokens**: {body.Secrets.Length}
               • 🧹 **Deleted Tokens**: {nAffected}
               • 🌍 **IP Address**: {remoteIP}
               • 📍 **Country**: {ipCountry}
               • 🆔 **Report ID**: `{reportId}`
               
               Please investigate further if necessary.
               """,
            Color.OrangeRed);

        return Ok();
    }
}