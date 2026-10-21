using System;

namespace Xdb.Api.Infrastructure.Data.Entities.Common;

/// <summary>Member (user) id that last modified the row.</summary>
public interface IHasLastModifiedBy : IHasLastModified
{
    Guid? LastModifiedBy { get; set; }
}
