using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class DebugLog : EntityBase<int>
{
    public string Category { get; set; } = null!;

    public string Message { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<DebugLog>
    {
        public void Configure(EntityTypeBuilder<DebugLog> builder)
        {
            builder.Property(e => e.Category).IsUnicode(false);
            builder.Property(e => e.Message).IsUnicode(false);
        }
    }
}
