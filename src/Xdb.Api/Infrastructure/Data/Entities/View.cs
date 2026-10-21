using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class View : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public string Name { get; set; } = null!;

    public string DisplayValue { get; set; } = null!;

    public string Description { get; set; } = null!;

    public Guid? AssetTypeId { get; set; }

    public string? DriverCaption { get; set; }

    public string? NewItemCaption { get; set; }

    public string? ConfirmationLabel { get; set; }

    public string? ReportingLabel { get; set; }

    public bool? IsReadOnly { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool IsInstance { get; set; }

    public bool IsStandAlone { get; set; }

    public bool IsCreation { get; set; }

    public string? FilterLogic { get; set; }

    public Guid? IconId { get; set; }

    public virtual AssetType? AssetType { get; set; }

    public virtual ICollection<AssetType> AssetTypeDefaultInstanceViews { get; set; } = new List<AssetType>();

    public virtual ICollection<AssetType> AssetTypeDefaultViews { get; set; } = new List<AssetType>();

    public virtual ICollection<RolesView> RolesViews { get; set; } = new List<RolesView>();

    public virtual ICollection<ViewFilter> ViewFilters { get; set; } = new List<ViewFilter>();

    public virtual ICollection<ViewsPropertyGroup> ViewsPropertyGroups { get; set; } = new List<ViewsPropertyGroup>();

    public class EntityConfiguration : IEntityTypeConfiguration<View>
    {
        public void Configure(EntityTypeBuilder<View> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Views_uiId");
            builder.Property(e => e.ConfirmationLabel)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Views_dtCreated")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.DriverCaption)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.FilterLogic).IsUnicode(false);
            builder.Property(e => e.IsReadOnly).HasDefaultValue(false, "DF_Views_IsReadOnly");
            builder.Property(e => e.IsStandAlone).HasDefaultValue(true, "DF_Views_IsStandAlone");
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.NewItemCaption)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.ReportingLabel)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.HasOne(d => d.AssetType).WithMany(p => p.Views)
                .HasForeignKey(d => d.AssetTypeId)
                .HasConstraintName("FK_Views_AssetTypes");
        }
    }
}
