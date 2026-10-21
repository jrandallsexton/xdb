using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class BulkUpload : EntityBase<Guid>, IHasCreatedBy
{
    public Guid FileId { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Completed { get; set; }

    public int? RecordCount { get; set; }

    public Guid? AssetTypeId { get; set; }

    public bool? IsInstance { get; set; }

    public int? ImportCount { get; set; }

    public int? UpdateCount { get; set; }

    public virtual ICollection<BulkUploadLog> BulkUploadLogs { get; set; } = new List<BulkUploadLog>();

    public virtual Document File { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<BulkUpload>
    {
        public void Configure(EntityTypeBuilder<BulkUpload> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Completed).HasColumnType("smalldatetime");
            builder.Property(e => e.Created).HasColumnType("smalldatetime");

            builder.HasOne(d => d.File).WithMany(p => p.BulkUploads)
                .HasForeignKey(d => d.FileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploads_Documents");
        }
    }
}
