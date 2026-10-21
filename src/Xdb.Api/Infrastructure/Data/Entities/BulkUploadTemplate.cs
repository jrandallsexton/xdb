using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class BulkUploadTemplate : EntityBase<Guid>
{
    public string Name { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<BulkUploadTemplate>
    {
        public void Configure(EntityTypeBuilder<BulkUploadTemplate> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Name)
                .HasMaxLength(200)
                .IsUnicode(false);
        }
    }
}
