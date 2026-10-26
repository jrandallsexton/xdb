using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PropertyValue : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid PropertyId { get; set; }

    public Guid AssetId { get; set; }

    public Guid? SubmittalGroupId { get; set; }

    public string? Value { get; set; }

    public int? Order { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public DateTime? Rejected { get; set; }

    public Guid? RejectedBy { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<PropertyValue>
    {
        public void Configure(EntityTypeBuilder<PropertyValue> builder)
        {
            builder.HasKey(e => e.Id)
                .HasName("PK_PropertyValues_1")
                .IsClustered(false);

            builder.HasIndex(e => e.AssetId, "IX_AssetIds");

            // A property's value on an asset: dbo.PropertyValue (every gen-table cell) and the value readers. Covers the
            // live, approved filter, so the lookup never goes to the heap.
            builder.HasIndex(e => new { e.PropertyId, e.AssetId, e.Deleted }, "IX_PropertyValues_Lookup")
                .IncludeProperties(e => new { e.Approved, e.Value });

            builder.HasIndex(e => e.SubmittalGroupId, "IX_PropertyValues_SubmittalGroupId");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_PropertyValues_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Rejected).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Asset).WithMany(p => p.PropertyValues)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PropertyValues_Assets");

            builder.HasOne(d => d.Property).WithMany(p => p.PropertyValues)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PropertyValues_Properties");
        }
    }
}
