using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ChartsProperty : EntityBase<Guid>
{
    public Guid ChartId { get; set; }

    public Guid? PropertyId { get; set; }

    public Guid? SubPropertyId { get; set; }

    public int Order { get; set; }

    public string? Label { get; set; }

    public string? Logic { get; set; }

    public bool IsGrouping { get; set; }

    public int? CustomReportFieldTypeId { get; set; }

    public int? TimeElapsedTypeId { get; set; }

    public int? TimeElapsedFormatId { get; set; }

    public DateTime? TimeElapsedSpecificDate { get; set; }

    public virtual Chart Chart { get; set; } = null!;

    public virtual Property? Property { get; set; }

    public virtual Property? SubProperty { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ChartsProperty>
    {
        public void Configure(EntityTypeBuilder<ChartsProperty> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Label)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Logic)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.TimeElapsedSpecificDate).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Chart).WithMany(p => p.ChartsProperties)
                .HasForeignKey(d => d.ChartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChartsProperties_Charts");

            builder.HasOne(d => d.Property).WithMany(p => p.ChartsPropertyProperties)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_ChartsProperties_Properties");

            builder.HasOne(d => d.SubProperty).WithMany(p => p.ChartsPropertySubProperties)
                .HasForeignKey(d => d.SubPropertyId)
                .HasConstraintName("FK_ChartsProperties_Properties1");
        }
    }
}
