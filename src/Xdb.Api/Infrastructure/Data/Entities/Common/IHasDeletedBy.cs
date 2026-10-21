using System;

namespace Xdb.Api.Infrastructure.Data.Entities.Common;

/// <summary>Member (user) id that soft-deleted the row.</summary>
public interface IHasDeletedBy : ISoftDeletable
{
    Guid? DeletedBy { get; set; }
}
