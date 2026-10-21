using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class CurrencyValue : EntityBase<Guid>
{
    public Guid CurrencySymbolId { get; set; }

    public decimal? Value { get; set; }

    public virtual CurrencySymbol CurrencySymbol { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<CurrencyValue>
    {
        public void Configure(EntityTypeBuilder<CurrencyValue> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Value).HasColumnType("decimal(18, 2)");

            builder.HasOne(d => d.CurrencySymbol).WithMany(p => p.CurrencyValues)
                .HasForeignKey(d => d.CurrencySymbolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CurrencyValues_CurrencySymbols");
        }
    }
}
