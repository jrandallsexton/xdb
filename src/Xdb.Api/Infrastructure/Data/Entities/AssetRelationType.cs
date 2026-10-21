using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetRelationType : EntityBase<byte>, IHasDeletedBy
{
    public string Name { get; set; } = null!;

    public DateTime? Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual ICollection<AssetRelation> AssetRelations { get; set; } = new List<AssetRelation>();

    public class EntityConfiguration : IEntityTypeConfiguration<AssetRelationType>
    {
        public void Configure(EntityTypeBuilder<AssetRelationType> builder)
        {
            builder.HasIndex(e => e.Name, "uc_Unique_AssetRelationTypes").IsUnique();

            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
