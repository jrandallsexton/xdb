using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class BulkUploadLog : EntityBase<int>, IHasCreated
{
    public Guid BulkUploadId { get; set; }

    public string Category { get; set; } = null!;

    public Guid? AssetId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime Created { get; set; }

    public int Order { get; set; }

    public virtual Asset? Asset { get; set; }

    public virtual BulkUpload BulkUpload { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<BulkUploadLog>
    {
        public void Configure(EntityTypeBuilder<BulkUploadLog> builder)
        {
            builder.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_BulkUploadLogs_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Message).IsUnicode(false);

            builder.HasOne(d => d.Asset).WithMany(p => p.BulkUploadLogs)
                .HasForeignKey(d => d.AssetId)
                .HasConstraintName("FK_BulkUploadLogs_Assets");

            builder.HasOne(d => d.BulkUpload).WithMany(p => p.BulkUploadLogs)
                .HasForeignKey(d => d.BulkUploadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadLogs_BulkUploads");
        }
    }
}
