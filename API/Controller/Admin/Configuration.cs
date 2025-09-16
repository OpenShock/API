using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Configuration;
using System.Net.Mime;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Gets all configuration items
    /// </summary>
    /// <response code="200"></response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("config")]
    [ProducesResponseType<IAsyncEnumerable<ConfigurationItemDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)] // Ok
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
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)] // Ok
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // AlreadyExists
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // InvalidNameFormat, InvalidValueFormat
    public async Task<IActionResult> ConfigurationAdd([FromBody] ConfigurationAddItemRequest body, [FromServices] IConfigurationService configurationService, CancellationToken cancellationToken)
    {
        var result = await configurationService.TryAddItemAsync(
            body.Name,
            body.Description,
            body.Type,
            body.Value
        );

        return result.Match<IActionResult>(
            success => Ok(),
            alreadyExists => Problem(ConfigurationError.AlreadyExists(body.Name)),
            invalidName => Problem(ConfigurationError.InvalidNameFormat(body.Name)),
            invalidValue => Problem(ConfigurationError.InvalidValueFormat(body.Value))
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
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)] // Ok
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // AlreadyExists
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // Not found
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // InvalidNameFormat, InvalidValueFormat
    public async Task<IActionResult> ConfigurationUpdate([FromBody] ConfigurationUpdateItemRequest body, [FromServices] IConfigurationService configurationService, CancellationToken cancellationToken)
    {
        var result = await configurationService.TryUpdateItemAsync(
            body.Name,
            body.Description,
            body.Value
        );

        return result.Match<IActionResult>(
            success => Ok(),
            notFound => Problem(ConfigurationError.NotFound(body.Name)),
            invalidName => Problem(ConfigurationError.InvalidNameFormat(body.Name)),
            invalidValue => Problem(ConfigurationError.InvalidValueFormat(body.Value!))
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
    [ProducesResponseType(StatusCodes.Status200OK)] // Deleted
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // Not found
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // InvalidNameFormat
    public async Task<IActionResult> ConfigurationDelete([FromRoute] string name, [FromServices] IConfigurationService configurationService, CancellationToken cancellationToken)
    {
        var result = await configurationService.TryDeleteItemAsync(name);

        return result.Match<IActionResult>(
            success => Ok(),
            notFound => Problem(ConfigurationError.NotFound(name)),
            invalidName => Problem(ConfigurationError.InvalidNameFormat(name))
        );
    }
}