using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MimeType : EntityBase<Guid>
{
    public string FileExtension { get; set; } = null!;

    public string MimeType1 { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<MimeType>
    {
        public void Configure(EntityTypeBuilder<MimeType> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_MimeTypes_Id");
            builder.Property(e => e.FileExtension)
                .HasMaxLength(10)
                .IsUnicode(false);
            builder.Property(e => e.MimeType1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("MimeType");
        }
    }
}
