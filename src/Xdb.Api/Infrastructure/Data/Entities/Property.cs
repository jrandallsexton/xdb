using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Property : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public byte DataTypeId { get; set; }

    public byte? Precision { get; set; }

    public Guid? PickListId { get; set; }

    public Guid? RoleId { get; set; }

    public bool IsOrdered { get; set; }

    public bool AllowMultiValue { get; set; }

    public string Name { get; set; } = null!;

    public string? DisplayValue { get; set; }

    public string? Description { get; set; }

    public Guid? AssetTypeId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool? AssetTypeIsInstance { get; set; }

    public bool IsSystem { get; set; }

    public int? SystemTypeId { get; set; }

    public string? Singular { get; set; }

    public string? Plural { get; set; }

    public virtual AssetType? AssetType { get; set; }

    public virtual ICollection<AssetTypesProperty> AssetTypesProperties { get; set; } = new List<AssetTypesProperty>();

    public virtual ICollection<ChartsFilter> ChartsFilters { get; set; } = new List<ChartsFilter>();

    public virtual ICollection<ChartsProperty> ChartsPropertyProperties { get; set; } = new List<ChartsProperty>();

    public virtual ICollection<ChartsProperty> ChartsPropertySubProperties { get; set; } = new List<ChartsProperty>();

    public virtual DataType DataType { get; set; } = null!;

    public virtual PickList? PickList { get; set; }

    public virtual ICollection<ProcessStepFilter> ProcessStepFilters { get; set; } = new List<ProcessStepFilter>();

    public virtual ICollection<PropertyGroupsProperty> PropertyGroupsProperties { get; set; } = new List<PropertyGroupsProperty>();

    public virtual ICollection<PropertyValue> PropertyValues { get; set; } = new List<PropertyValue>();

    public virtual ICollection<ReportsFilter> ReportsFilters { get; set; } = new List<ReportsFilter>();

    public virtual ICollection<ReportsProperty> ReportsPropertyProperties { get; set; } = new List<ReportsProperty>();

    public virtual ICollection<ReportsProperty> ReportsPropertySubProperties { get; set; } = new List<ReportsProperty>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<RolesAssetTypesFilter> RolesAssetTypesFilters { get; set; } = new List<RolesAssetTypesFilter>();

    public virtual SystemType? SystemType { get; set; }

    public virtual ICollection<ViewFilter> ViewFilters { get; set; } = new List<ViewFilter>();

    public class EntityConfiguration : IEntityTypeConfiguration<Property>
    {
        public void Configure(EntityTypeBuilder<Property> builder)
        {
            builder.HasIndex(e => e.DataTypeId, "IX_DataTypeIds");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Properties_uiId");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Properties_dtCreated")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Plural)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Singular)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.HasOne(d => d.AssetType).WithMany(p => p.Properties)
                .HasForeignKey(d => d.AssetTypeId)
                .HasConstraintName("FK_Properties_AssetTypes");

            builder.HasOne(d => d.DataType).WithMany(p => p.Properties)
                .HasForeignKey(d => d.DataTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Properties_DataTypes");

            builder.HasOne(d => d.PickList).WithMany(p => p.Properties)
                .HasForeignKey(d => d.PickListId)
                .HasConstraintName("FK_Properties_PickLists1");

            builder.HasOne(d => d.Role).WithMany(p => p.Properties)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_Properties_Roles");

            builder.HasOne(d => d.SystemType).WithMany(p => p.Properties)
                .HasForeignKey(d => d.SystemTypeId)
                .HasConstraintName("FK_Properties_SystemTypes");
        }
    }
}
