using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ReportsProperty : EntityBase<Guid>
{
    public Guid ReportId { get; set; }

    public Guid? PropertyId { get; set; }

    public int Order { get; set; }

    public bool IsGrouping { get; set; }

    public Guid? SubPropertyId { get; set; }

    public int? CustomReportFieldTypeId { get; set; }

    public int? TimeElapsedTypeId { get; set; }

    public int? TimeElapsedFormatId { get; set; }

    public DateTime? TimeElapsedSpecificDate { get; set; }

    public string? Label { get; set; }

    public string? Logic { get; set; }

    public virtual Property? Property { get; set; }

    public virtual Report Report { get; set; } = null!;

    public virtual Property? SubProperty { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ReportsProperty>
    {
        public void Configure(EntityTypeBuilder<ReportsProperty> builder)
        {
            builder.HasIndex(e => e.PropertyId, "IX_PropertyIds");

            builder.HasIndex(e => e.ReportId, "IX_ReportIds");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Label)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Logic)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.TimeElapsedSpecificDate).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Property).WithMany(p => p.ReportsPropertyProperties)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_ReportsProperties_Properties");

            builder.HasOne(d => d.Report).WithMany(p => p.ReportsProperties)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportsProperties_Reports");

            builder.HasOne(d => d.SubProperty).WithMany(p => p.ReportsPropertySubProperties)
                .HasForeignKey(d => d.SubPropertyId)
                .HasConstraintName("FK_ReportsProperties_Properties1");
        }
    }
}
