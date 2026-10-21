using System;

namespace Xdb.Api.Infrastructure.Data.Entities.Common;

/// <summary>
/// Row creation time. Local server time, set by a <c>getdate()</c> default in the original schema (not UTC).
/// Tables whose <c>Created</c> column is nullable do not implement this.
/// </summary>
public interface IHasCreated
{
    DateTime Created { get; set; }
}
