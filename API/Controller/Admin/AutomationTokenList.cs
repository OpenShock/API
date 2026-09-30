using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Lists all automation tokens
    /// </summary>
    [HttpGet("automationTokens")]
    public async IAsyncEnumerable<AutomationTokenDto> ListAutomationTokens()
    {
        PedanticallyEnsureAdmin();

        await foreach (var token in _db.AutomationTokens.AsNoTracking().AsAsyncEnumerable())
        {
            yield return AutomationTokenDto.FromEntity(token);
        }
    }
}
