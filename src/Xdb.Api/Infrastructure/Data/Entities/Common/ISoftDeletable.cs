using System;

namespace Xdb.Api.Infrastructure.Data.Entities.Common;

/// <summary>
/// Soft delete: a non-null <c>Deleted</c> time marks the row as deleted.
/// No global query filter is applied: several original read paths include deleted rows on purpose.
/// </summary>
public interface ISoftDeletable
{
    DateTime? Deleted { get; set; }
}
