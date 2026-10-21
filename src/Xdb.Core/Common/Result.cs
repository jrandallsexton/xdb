using System.Collections.Generic;

using FluentValidation.Results;

namespace Xdb.Core.Common;

public abstract class Result<T>(T value, ResultStatus status)
{
    public T Value { get; } = value;

    public ResultStatus Status { get; } = status;

    public bool IsSuccess => this is Success<T>;
}

public class Failure<T>(T value, ResultStatus status, List<ValidationFailure> errors) :
    Result<T>(value, status)
{
    public List<ValidationFailure> Errors { get; } = errors;
}

public class Success<T>(T value, ResultStatus status = ResultStatus.Success) : Result<T>(value, status);

public enum ResultStatus
{
    Accepted,
    BadRequest,
    Conflict,
    Created,
    Error,
    Forbid,
    NotFound,
    RateLimited,
    Success,
    Unauthorized,
    Validation

    // Append new members at the end: members number by position, so inserting
    // mid-enum silently remaps any value that was ever serialized numerically.
}
