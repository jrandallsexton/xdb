using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PropertyValueSubmittal : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid? AssetId { get; set; }

    public Guid? PickListValueId { get; set; }

    public string? Notes { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Reviewed { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public DateTime? TriggerChecked { get; set; }

    public string? Narrative { get; set; }

    public virtual Asset? Asset { get; set; }

    public virtual PickListValue? PickListValue { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<PropertyValueSubmittal>
    {
        public void Configure(EntityTypeBuilder<PropertyValueSubmittal> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Narrative)
                .HasMaxLength(255)
                .IsUnicode(false);
            builder.Property(e => e.Notes).IsUnicode(false);
            builder.Property(e => e.Reviewed).HasColumnType("smalldatetime");
            builder.Property(e => e.TriggerChecked).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Asset).WithMany(p => p.PropertyValueSubmittals)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK_PropertyValueSubmittals_Assets");

            builder.HasOne(d => d.PickListValue).WithMany(p => p.PropertyValueSubmittals)
                .HasForeignKey(d => d.PickListValueId)
                .HasConstraintName("FK_PropertyValueSubmittals_PickListValues");
        }
    }
}
