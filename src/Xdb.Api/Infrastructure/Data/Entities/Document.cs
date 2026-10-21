using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Document : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public int DocumentTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public byte[]? Data { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual ICollection<BulkUpload> BulkUploads { get; set; } = new List<BulkUpload>();

    public virtual DocumentType DocumentType { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Documents_Id");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Documents_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false);

            builder.HasOne(d => d.DocumentType).WithMany(p => p.Documents)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_DocumentTypes");
        }
    }
}
