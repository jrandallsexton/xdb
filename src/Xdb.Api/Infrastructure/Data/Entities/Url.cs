using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Url : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Url1 { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Url>
    {
        public void Configure(EntityTypeBuilder<Url> builder)
        {
            builder.ToTable("URLs");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Url1)
                .IsUnicode(false)
                .HasColumnName("URL");
        }
    }
}
