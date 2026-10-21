using System;
using System.Collections.Generic;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ViewClient : EntityBase<int>
{
    public Guid ViewId { get; set; }

    public Guid UserId { get; set; }

    public byte[] Data { get; set; } = null!;
}
