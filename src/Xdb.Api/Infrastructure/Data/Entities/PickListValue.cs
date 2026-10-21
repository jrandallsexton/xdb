using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PickListValue : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public Guid PickListId { get; set; }

    public string Value { get; set; } = null!;

    public string? DisplayValue { get; set; }

    public string? Bgcolor { get; set; }

    public int Order { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual PickList PickList { get; set; } = null!;

    public virtual ICollection<PickListDependency> PickListDependencyChildPickListValues { get; set; } = new List<PickListDependency>();

    public virtual ICollection<PickListDependency> PickListDependencyParentPickListValues { get; set; } = new List<PickListDependency>();

    public virtual ICollection<PropertyValueSubmittal> PropertyValueSubmittals { get; set; } = new List<PropertyValueSubmittal>();

    public class EntityConfiguration : IEntityTypeConfiguration<PickListValue>
    {
        public void Configure(EntityTypeBuilder<PickListValue> builder)
        {
            builder.HasIndex(e => e.PickListId, "IX_PickListId");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_PickListValues_uiId");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Bgcolor)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("BGColor");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_PickListValues_dtCreated")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(255)
                .IsUnicode(false);
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false);

            builder.HasOne(d => d.PickList).WithMany(p => p.PickListValues)
                .HasForeignKey(d => d.PickListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PickListValues_PickLists1");
        }
    }
}
