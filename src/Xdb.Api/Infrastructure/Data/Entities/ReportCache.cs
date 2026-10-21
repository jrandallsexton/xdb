using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ReportCache : IHasCreated, IHasLastModified
{
    public Guid ReportId { get; set; }

    public Guid DocumentId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? LastModified { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ReportCache>
    {
        public void Configure(EntityTypeBuilder<ReportCache> builder)
        {
            builder.HasKey(e => e.ReportId);

            builder.ToTable("ReportCache");

            builder.Property(e => e.ReportId).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
        }
    }
}
