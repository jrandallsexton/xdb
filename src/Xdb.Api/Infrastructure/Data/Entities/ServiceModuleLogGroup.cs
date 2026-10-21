using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ServiceModuleLogGroup : EntityBase<Guid>
{
    public Guid ServiceModuleId { get; set; }

    public int? LogCount { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Completed { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ServiceModuleLogGroup>
    {
        public void Configure(EntityTypeBuilder<ServiceModuleLogGroup> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Completed).HasColumnType("smalldatetime");
            builder.Property(e => e.Started).HasColumnType("smalldatetime");
        }
    }
}
