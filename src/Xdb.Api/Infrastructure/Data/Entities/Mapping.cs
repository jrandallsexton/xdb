using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Mapping : EntityBase<Guid>
{
    public Guid AssetTypeId { get; set; }

    public string Key { get; set; } = null!;

    public Guid? Value { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Mapping>
    {
        public void Configure(EntityTypeBuilder<Mapping> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_SoftwareMappings");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_SoftwareMappings_Id");
            builder.Property(e => e.Key).IsUnicode(false);
        }
    }
}
