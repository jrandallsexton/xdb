using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class DatePart : EntityBase<Guid>
{
    public string Value { get; set; } = null!;

    public string SqlDatePart { get; set; } = null!;

    public string DisplayValue { get; set; } = null!;

    public int? Order { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<DatePart>
    {
        public void Configure(EntityTypeBuilder<DatePart> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_DateParts_Id");
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.SqlDatePart)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Value)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
