using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class CurrencySymbol : EntityBase<Guid>
{
    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string Display { get; set; } = null!;

    public virtual ICollection<CurrencyValue> CurrencyValues { get; set; } = new List<CurrencyValue>();

    public class EntityConfiguration : IEntityTypeConfiguration<CurrencySymbol>
    {
        public void Configure(EntityTypeBuilder<CurrencySymbol> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Display).HasMaxLength(50);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Value)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
