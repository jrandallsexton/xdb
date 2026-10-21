using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ImageType : EntityBase<byte>, ISoftDeletable
{
    public string Name { get; set; } = null!;

    public DateTime? Created { get; set; }

    public DateTime? Deleted { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public class EntityConfiguration : IEntityTypeConfiguration<ImageType>
    {
        public void Configure(EntityTypeBuilder<ImageType> builder)
        {
            builder.HasIndex(e => e.Name, "uc_Unique_ImageTypes").IsUnique();

            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
