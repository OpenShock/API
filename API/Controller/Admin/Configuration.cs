using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Services.Configuration;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Gets all configuration items
    /// </summary>
    /// <response code="200"></response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("config")]
    public IAsyncEnumerable<ConfigurationItemDto> ConfigurationList([FromServices] IConfigurationService configurationService)
    {
        return configurationService
            .GetAllItemsQuery()
            .Select(ci => new ConfigurationItemDto
            {
                Name = ci.Name,
                Description = ci.Description,
                Type = ci.Type,
                Value = ci.Value,
                UpdatedAt = ci.UpdatedAt,
                CreatedAt = ci.CreatedAt,
            })
            .AsAsyncEnumerable();
    }

    /// <summary>
    /// Adds a new configuration
    /// </summary>
    /// <response code="200">Configuration added</response>
    /// <response code="400">Invalid name or value format</response>
    /// <response code="409">Already exists</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("config")]
    public async Task<IActionResult> ConfigurationAdd(
        [FromBody] ConfigurationAddItemRequest body,
        [FromServices] IConfigurationService configurationService,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.TryAddItemAsync(
            body.Name,
            body.Description,
            body.Type,
            body.Value
        );

        return result.Match<IActionResult>(
            success => Ok(),
            alreadyExists => Conflict(new { Error = "AlreadyExists", Message = $"A configuration named '{body.Name}' already exists." }),
            invalidName => BadRequest(new { Error = "InvalidNameFormat", Message = $"Invalid configuration name: '{body.Name}'. Only A–Z and '_' allowed." }),
            invalidValue => BadRequest(new { Error = "InvalidValueFormat", Message = $"Value '{body.Value}' is not a valid {body.Type}." })
        );
    }

    /// <summary>
    /// Updates an existing configuration
    /// </summary>
    /// <response code="200">Configuration updated (or nothing to do)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="400">Invalid name or value format or type mismatch</response>
    /// <response code="404">Not found</response>
    [HttpPut("config")]
    public async Task<IActionResult> ConfigurationUpdate(
        [FromBody] ConfigurationUpdateItemRequest body,
        [FromServices] IConfigurationService configurationService,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.TryUpdateItemAsync(
            body.Name,
            body.Description,
            body.Value
        );

        return result.Match<IActionResult>(
            success => Ok(),
            notFound => NotFound(new { Error = "NotFound", Message = $"No configuration named '{body.Name}'." }),
            invalidName => BadRequest(new { Error = "InvalidNameFormat", Message = $"Invalid configuration name: '{body.Name}'." }),
            invalidValue => BadRequest(new { Error = "InvalidValueFormat", Message = $"Value '{body.Value}' is not a valid format for '{body.Name}'." }),
            invalidType => BadRequest(new { Error = "InvalidValueType", Message = $"Type mismatch when updating '{body.Name}'." })
        );
    }

    /// <summary>
    /// Deletes a configuration
    /// </summary>
    /// <response code="200">Configuration deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="400">Invalid name format</response>
    /// <response code="404">Not found</response>
    [HttpDelete("config/{name}")]
    public async Task<IActionResult> ConfigurationDelete(
        string name,
        [FromServices] IConfigurationService configurationService,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.TryDeleteItemAsync(name);

        return result.Match<IActionResult>(
            success => Ok(),
            notFound => NotFound(new { Error = "NotFound", Message = $"No configuration named '{name}'." }),
            invalidName => BadRequest(new { Error = "InvalidNameFormat", Message = $"Invalid configuration name: '{name}'." })
        );
    }
}