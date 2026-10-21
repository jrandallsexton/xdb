using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class LkOperator : EntityBase<int>
{
    public string Operator { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<LkOperator>
    {
        public void Configure(EntityTypeBuilder<LkOperator> builder)
        {
            builder.ToTable("lkOperators");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Operator)
                .HasMaxLength(5)
                .IsUnicode(false);
        }
    }
}
