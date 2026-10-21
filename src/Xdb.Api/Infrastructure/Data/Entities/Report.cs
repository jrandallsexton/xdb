using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Report : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public string ReportName { get; set; } = null!;

    public Guid? AssetTypeId { get; set; }

    public string? FilterExpression { get; set; }

    public bool IsInstance { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public string? StoredProc { get; set; }

    public string? FilterString { get; set; }

    public virtual AssetType? AssetType { get; set; }

    public virtual ICollection<MemberBookmark> MemberBookmarks { get; set; } = new List<MemberBookmark>();

    public virtual ICollection<ReportLog> ReportLogs { get; set; } = new List<ReportLog>();

    public virtual ICollection<ReportsFilter> ReportsFilters { get; set; } = new List<ReportsFilter>();

    public virtual ICollection<ReportsProperty> ReportsProperties { get; set; } = new List<ReportsProperty>();

    public virtual ICollection<RolesReport> RolesReports { get; set; } = new List<RolesReport>();

    public class EntityConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.FilterExpression).IsUnicode(false);
            builder.Property(e => e.FilterString).IsUnicode(false);
            builder.Property(e => e.ReportName)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.StoredProc)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.HasOne(d => d.AssetType).WithMany(p => p.Reports)
                .HasForeignKey(d => d.AssetTypeId)
                .HasConstraintName("FK_Reports_AssetTypes");
        }
    }
}
