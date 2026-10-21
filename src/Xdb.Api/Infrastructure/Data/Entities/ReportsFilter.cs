using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ReportsFilter : EntityBase<Guid>
{
    public Guid ReportId { get; set; }

    public Guid PropertyId { get; set; }

    public string Value { get; set; } = null!;

    public int Order { get; set; }

    public int? OperatorId { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual Report Report { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<ReportsFilter>
    {
        public void Configure(EntityTypeBuilder<ReportsFilter> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ReportsFilters_Id");
            builder.Property(e => e.Value).IsUnicode(false);

            builder.HasOne(d => d.Property).WithMany(p => p.ReportsFilters)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportsFilters_Properties");

            builder.HasOne(d => d.Report).WithMany(p => p.ReportsFilters)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportsFilters_Reports");
        }
    }
}
