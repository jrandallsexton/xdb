using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetTypeRelation : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid ParentAssetTypeId { get; set; }

    public Guid ChildAssetTypeId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual AssetType ChildAssetType { get; set; } = null!;

    public virtual AssetType ParentAssetType { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<AssetTypeRelation>
    {
        public void Configure(EntityTypeBuilder<AssetTypeRelation> builder)
        {
            builder.HasIndex(e => new { e.ParentAssetTypeId, e.ChildAssetTypeId, e.Deleted }, "uc_ParentChild").IsUnique();

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_AssetTypeRelations_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AssetTypeRelations_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.ChildAssetType).WithMany(p => p.AssetTypeRelationChildAssetTypes)
                .HasForeignKey(d => d.ChildAssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTypeRelations_AssetTypes1");

            builder.HasOne(d => d.ParentAssetType).WithMany(p => p.AssetTypeRelationParentAssetTypes)
                .HasForeignKey(d => d.ParentAssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTypeRelations_AssetTypes");
        }
    }
}
