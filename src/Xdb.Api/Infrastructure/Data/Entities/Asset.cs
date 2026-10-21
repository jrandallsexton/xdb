using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Asset : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public Guid AssetTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? DisplayValue { get; set; }

    public string? Description { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool IsInstance { get; set; }

    public Guid? InstanceOfId { get; set; }

    public Guid? DatastoreId { get; set; }

    public virtual ICollection<AssetHit> AssetHits { get; set; } = new List<AssetHit>();

    public virtual ICollection<AssetRelation> AssetRelationFromAssets { get; set; } = new List<AssetRelation>();

    public virtual ICollection<AssetRelation> AssetRelationToAssets { get; set; } = new List<AssetRelation>();

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual ICollection<BulkUploadLog> BulkUploadLogs { get; set; } = new List<BulkUploadLog>();

    public virtual Asset? InstanceOf { get; set; }

    public virtual ICollection<Asset> InverseInstanceOf { get; set; } = new List<Asset>();

    public virtual ICollection<PropertyValueSubmittal> PropertyValueSubmittals { get; set; } = new List<PropertyValueSubmittal>();

    public virtual ICollection<PropertyValue> PropertyValues { get; set; } = new List<PropertyValue>();

    public class EntityConfiguration : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            builder.HasIndex(e => e.Name, "IX_AssetNames");

            builder.HasIndex(e => e.AssetTypeId, "IX_AssetTypeIds");

            builder.HasIndex(e => new { e.AssetTypeId, e.IsInstance }, "_dta_index_Assets_5_1365579903__K2_K14_1_3_4_15");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Assets_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false);

            builder.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assets_AssetTypes");

            builder.HasOne(d => d.InstanceOf).WithMany(p => p.InverseInstanceOf)
                .HasForeignKey(d => d.InstanceOfId)
                .HasConstraintName("FK_Assets_Assets");
        }
    }
}
