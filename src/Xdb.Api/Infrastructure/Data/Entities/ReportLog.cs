using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ReportLog : EntityBase<Guid>, IHasCreated, ISoftDeletable
{
    public Guid ReportId { get; set; }

    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public TimeOnly Started { get; set; }

    public TimeOnly? Completed { get; set; }

    public string? SqlText { get; set; }

    public int? RecordsReturned { get; set; }

    public DateTime? Deleted { get; set; }

    public bool? WasCached { get; set; }

    public virtual Report Report { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<ReportLog>
    {
        public void Configure(EntityTypeBuilder<ReportLog> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_ReportLogs_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.SqlText).IsUnicode(false);
            builder.Property(e => e.WasCached).HasDefaultValue(false, "DF_ReportLogs_WasCached");

            builder.HasOne(d => d.Report).WithMany(p => p.ReportLogs)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportLogs_Reports");
        }
    }
}
