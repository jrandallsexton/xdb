using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetRelation : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid FromAssetId { get; set; }

    public Guid ToAssetId { get; set; }

    public byte AssetRelationTypeId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual AssetRelationType AssetRelationType { get; set; } = null!;

    public virtual Asset FromAsset { get; set; } = null!;

    public virtual Asset ToAsset { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<AssetRelation>
    {
        public void Configure(EntityTypeBuilder<AssetRelation> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_AssetRelations_1");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_AssetRelations_Id");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AssetRelations_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.AssetRelationType).WithMany(p => p.AssetRelations)
                .HasForeignKey(d => d.AssetRelationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetRelations_AssetRelationTypes");

            builder.HasOne(d => d.FromAsset).WithMany(p => p.AssetRelationFromAssets)
                .HasForeignKey(d => d.FromAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetRelations_Assets");

            builder.HasOne(d => d.ToAsset).WithMany(p => p.AssetRelationToAssets)
                .HasForeignKey(d => d.ToAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetRelations_Assets1");
        }
    }
}
