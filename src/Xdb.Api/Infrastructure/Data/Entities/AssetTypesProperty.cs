using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetTypesProperty : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid AssetTypeId { get; set; }

    public Guid PropertyId { get; set; }

    public bool IsInstance { get; set; }

    public bool IsInherited { get; set; }

    public bool IsInheritedValue { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<AssetTypesProperty>
    {
        public void Configure(EntityTypeBuilder<AssetTypesProperty> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AssetTypesProperties_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.AssetType).WithMany(p => p.AssetTypesProperties)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTypesProperties_AssetTypes");

            builder.HasOne(d => d.Property).WithMany(p => p.AssetTypesProperties)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTypesProperties_Properties");
        }
    }
}
