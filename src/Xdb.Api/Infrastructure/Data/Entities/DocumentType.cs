using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class DocumentType : EntityBase<int>, ISoftDeletable
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime? Created { get; set; }

    public DateTime? Deleted { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public class EntityConfiguration : IEntityTypeConfiguration<DocumentType>
    {
        public void Configure(EntityTypeBuilder<DocumentType> builder)
        {
            builder.HasIndex(e => e.Name, "uc_Unique_DocTypes").IsUnique();

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
