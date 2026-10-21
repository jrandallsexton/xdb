using System;

namespace Xdb.Api.Infrastructure.Data.Entities.Common;

/// <summary>Last modification time (local server time, as written by the original code).</summary>
public interface IHasLastModified
{
    DateTime? LastModified { get; set; }
}
