using System.Collections.Generic;

using FluentAssertions;

using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;

using Xdb.Core.Common;
using Xdb.Core.Extensions;

using Xunit;

namespace Xdb.Core.Tests.Unit.Extensions;

public class ResultExtensionsTests
{
    [Fact]
    public void Success_MapsTo200WithValue()
    {
        var actionResult = new Success<int>(42).ToActionResult();

        actionResult.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(42);
    }

    [Theory]
    [InlineData(ResultStatus.Created, 201)]
    [InlineData(ResultStatus.Accepted, 202)]
    public void Success_WithStatus_MapsToMatchingCode(ResultStatus status, int expected)
    {
        var actionResult = new Success<string>("x", status).ToActionResult();

        actionResult.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(ResultStatus.BadRequest, 400)]
    [InlineData(ResultStatus.Validation, 400)]
    [InlineData(ResultStatus.Unauthorized, 401)]
    [InlineData(ResultStatus.NotFound, 404)]
    [InlineData(ResultStatus.Conflict, 409)]
    [InlineData(ResultStatus.RateLimited, 429)]
    [InlineData(ResultStatus.Error, 500)]
    public void Failure_MapsToMatchingCode(ResultStatus status, int expected)
    {
        var failure = new Failure<string>(default!, status, [new ValidationFailure("Id", "bad")]);

        var actionResult = failure.ToActionResult();

        actionResult.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(expected);
    }

    [Fact]
    public void Failure_Forbid_MapsToForbidResult()
    {
        var failure = new Failure<string>(default!, ResultStatus.Forbid, new List<ValidationFailure>());

        failure.ToActionResult().Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public void OnSuccessProjection_IsUsedOnlyForSuccess()
    {
        var projected = new Success<int>(7).ToActionResult(v => new ObjectResult(v * 2) { StatusCode = 202 });
        var failed = new Failure<int>(0, ResultStatus.NotFound, []).ToActionResult(v => new OkResult());

        projected.Should().BeOfType<ObjectResult>().Which.Value.Should().Be(14);
        failed.Should().BeOfType<NotFoundObjectResult>();
    }
}
