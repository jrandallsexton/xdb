using System;

namespace Xdb.Api.Infrastructure.Data.Entities.Common;

/// <summary>Member (user) id that created the row.</summary>
public interface IHasCreatedBy : IHasCreated
{
    Guid CreatedBy { get; set; }
}
