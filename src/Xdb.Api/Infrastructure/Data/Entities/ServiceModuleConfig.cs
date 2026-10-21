using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ServiceModuleConfig : EntityBase<Guid>, IHasCreated, ISoftDeletable
{
    public Guid ServiceModuleId { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ServiceModuleConfig>
    {
        public void Configure(EntityTypeBuilder<ServiceModuleConfig> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Key)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Value).IsUnicode(false);
        }
    }
}
