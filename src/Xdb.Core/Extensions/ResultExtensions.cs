using System;

using Microsoft.AspNetCore.Mvc;

using Xdb.Core.Common;

namespace Xdb.Core.Extensions;

/// <summary>
/// Maps <see cref="Result{T}"/> to HTTP for the clean REST surface.
/// Legacy wire controllers do NOT use this; they format through the legacy wire formatters instead.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Projects a successful value through <paramref name="onSuccess"/> while routing
    /// failures through the shared status mapping.
    /// </summary>
    public static ActionResult ToActionResult<T>(this Result<T> result, Func<T, ActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);

        return result.ToActionResult().Result ?? new StatusCodeResult(500);
    }

    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Status switch
            {
                ResultStatus.Created => new CreatedResult((string?)null, result.Value),
                ResultStatus.Accepted => new AcceptedResult((string?)null, result.Value),
                _ => new OkObjectResult(result.Value)
            };
        }

        if (result is Failure<T> failure)
        {
            return result.Status switch
            {
                ResultStatus.BadRequest => new BadRequestObjectResult(new { failure.Errors }),
                ResultStatus.Validation => new BadRequestObjectResult(new { failure.Errors }),
                ResultStatus.Unauthorized => new UnauthorizedObjectResult(new { failure.Errors }),
                ResultStatus.Forbid => new ForbidResult(),
                ResultStatus.NotFound => new NotFoundObjectResult(new { failure.Errors }),
                ResultStatus.Conflict => new ConflictObjectResult(new { failure.Errors }),
                ResultStatus.RateLimited => new ObjectResult(new { failure.Errors }) { StatusCode = 429 },
                _ => new ObjectResult(new { failure.Errors }) { StatusCode = 500 }
            };
        }

        return new StatusCodeResult(500);
    }
}
