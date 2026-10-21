using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class StoredProcActivity : EntityBase<int>
{
    public string SpName { get; set; } = null!;

    public int Hits { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<StoredProcActivity>
    {
        public void Configure(EntityTypeBuilder<StoredProcActivity> builder)
        {
            builder.ToTable("StoredProcActivity");

            builder.Property(e => e.SpName)
                .HasMaxLength(255)
                .IsUnicode(false);
        }
    }
}
