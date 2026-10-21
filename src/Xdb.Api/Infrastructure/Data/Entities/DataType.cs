using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class DataType : EntityBase<byte>, IHasCreated, ISoftDeletable
{
    public string Value { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public class EntityConfiguration : IEntityTypeConfiguration<DataType>
    {
        public void Configure(EntityTypeBuilder<DataType> builder)
        {
            builder.HasIndex(e => e.Value, "uc_Unique_DataTypes").IsUnique();

            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_DataTypes_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Value)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
