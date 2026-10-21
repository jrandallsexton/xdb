using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetType : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public string Name { get; set; } = null!;

    public string? Pluralization { get; set; }

    public Guid? DefaultViewId { get; set; }

    public Guid? DefaultInstanceViewId { get; set; }

    public Guid? ImageId { get; set; }

    public string? DefinitionLabel { get; set; }

    public string? InstanceLabel { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public bool AllowAssets { get; set; }

    public string? DefinitionLabelPlural { get; set; }

    public string? InstanceLabelPlural { get; set; }

    public bool IsSystem { get; set; }

    public virtual ICollection<AssetTypeRelation> AssetTypeRelationChildAssetTypes { get; set; } = new List<AssetTypeRelation>();

    public virtual ICollection<AssetTypeRelation> AssetTypeRelationParentAssetTypes { get; set; } = new List<AssetTypeRelation>();

    public virtual ICollection<AssetTypesProperty> AssetTypesProperties { get; set; } = new List<AssetTypesProperty>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual View? DefaultInstanceView { get; set; }

    public virtual View? DefaultView { get; set; }

    public virtual Image? Image { get; set; }

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<PropertyGroup> PropertyGroups { get; set; } = new List<PropertyGroup>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<RolesAssetType> RolesAssetTypes { get; set; } = new List<RolesAssetType>();

    public virtual ICollection<RolesAssetTypesFilter> RolesAssetTypesFilters { get; set; } = new List<RolesAssetTypesFilter>();

    public virtual ICollection<RolesAssetTypesPermission> RolesAssetTypesPermissions { get; set; } = new List<RolesAssetTypesPermission>();

    public virtual ICollection<RolesAssetTypesProcess> RolesAssetTypesProcesses { get; set; } = new List<RolesAssetTypesProcess>();

    public virtual ICollection<View> Views { get; set; } = new List<View>();

    public class EntityConfiguration : IEntityTypeConfiguration<AssetType>
    {
        public void Configure(EntityTypeBuilder<AssetType> builder)
        {
            builder.HasIndex(e => e.Name, "uc_Unique_AssetTypes").IsUnique();

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.AllowAssets).HasDefaultValue(true, "DF_AssetTypes_AllowAssets");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AssetTypes_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.DefinitionLabel)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.DefinitionLabelPlural)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.InstanceLabel)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.InstanceLabelPlural)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Pluralization)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.HasOne(d => d.DefaultInstanceView).WithMany(p => p.AssetTypeDefaultInstanceViews)
                .HasForeignKey(d => d.DefaultInstanceViewId)
                .HasConstraintName("FK_AssetTypes_Views1");

            builder.HasOne(d => d.DefaultView).WithMany(p => p.AssetTypeDefaultViews)
                .HasForeignKey(d => d.DefaultViewId)
                .HasConstraintName("FK_AssetTypes_Views");

            builder.HasOne(d => d.Image).WithMany(p => p.AssetTypes)
                .HasForeignKey(d => d.ImageId)
                .HasConstraintName("FK_AssetTypes_Images");
        }
    }
}
