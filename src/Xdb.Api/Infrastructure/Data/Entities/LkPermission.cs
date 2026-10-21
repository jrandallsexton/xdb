using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class LkPermission : EntityBase<int>
{
    public string Name { get; set; } = null!;

    public string DisplayValue { get; set; } = null!;

    public string? Description { get; set; }

    public bool AllowFiltering { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<LkPermission>
    {
        public void Configure(EntityTypeBuilder<LkPermission> builder)
        {
            builder.HasKey(e => e.Id).HasFillFactor(80);

            builder.ToTable("lkPermissions");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.DisplayValue).IsUnicode(false);
            builder.Property(e => e.Name).IsUnicode(false);
        }
    }
}
