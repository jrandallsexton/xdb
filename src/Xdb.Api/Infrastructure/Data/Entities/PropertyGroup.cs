using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PropertyGroup : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public string Name { get; set; } = null!;

    public string DisplayValue { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public Guid AssetTypeId { get; set; }

    public bool IsInstance { get; set; }

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual ICollection<PropertyGroupsProperty> PropertyGroupsProperties { get; set; } = new List<PropertyGroupsProperty>();

    public virtual ICollection<ViewsPropertyGroup> ViewsPropertyGroups { get; set; } = new List<ViewsPropertyGroup>();

    public class EntityConfiguration : IEntityTypeConfiguration<PropertyGroup>
    {
        public void Configure(EntityTypeBuilder<PropertyGroup> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_Categories");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Categories_uiId");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Categories_dtCreated")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.HasOne(d => d.AssetType).WithMany(p => p.PropertyGroups)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PropertyGroups_AssetTypes");
        }
    }
}
