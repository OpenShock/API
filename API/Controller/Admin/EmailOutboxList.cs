using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Query;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Z.EntityFramework.Plus;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Lists email outbox messages, paginated. Supports the same OData-style
    /// <c>$filter</c>/<c>$orderby</c> as the users listing (e.g. <c>status eq 'failed'</c>,
    /// <c>recipient ilike '%@example.com'</c>). Defaults to newest first.
    /// </summary>
    /// <response code="200">Paginated email outbox messages</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("email/outbox")]
    [ProducesResponseType<Paginated<EmailOutboxMessageDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetEmailOutbox(
        [FromQuery(Name = "$filter")] string filterQuery = "",
        [FromQuery(Name = "$orderby")] string orderbyQuery = "",
        [FromQuery(Name = "$offset")][Range(0, int.MaxValue)] int offset = 0,
        [FromQuery(Name = "$limit")][Range(1, 1000)] int limit = 100
        )
    {
        PedanticallyEnsureAdmin();

        var query = _db.EmailOutbox.AsNoTracking();

        try
        {
            if (!string.IsNullOrEmpty(filterQuery))
            {
                query = query.ApplyFilter(filterQuery);
            }

            if (!string.IsNullOrEmpty(orderbyQuery))
            {
                query = query.ApplyOrderBy(orderbyQuery);
            }
            else
            {
                query = query.OrderByDescending(m => m.CreatedAt);
            }
        }
        catch (QueryStringTokenizerException e)
        {
            return Problem(ExpressionError.QueryStringInvalidError(e.Message));
        }
        catch (DBExpressionBuilderException e)
        {
            return Problem(ExpressionError.ExpressionExceptionError(e.Message));
        }
        catch (FormatException e)
        {
            return Problem(ExpressionError.ExpressionExceptionError(e.Message));
        }

        var deferredCount = query.DeferredLongCount().FutureValue();

        if (offset != 0)
        {
            query = query.Skip(offset);
        }

        var deferredMessages = query.Take(limit).Future();

        return Ok(new Paginated<EmailOutboxMessageDto>
        {
            Data = (await deferredMessages.ToArrayAsync()).Select(EmailOutboxMessageDto.FromModel).ToArray(),
            Offset = offset,
            Limit = limit,
            Total = await deferredCount.ValueAsync(),
        });
    }
}
