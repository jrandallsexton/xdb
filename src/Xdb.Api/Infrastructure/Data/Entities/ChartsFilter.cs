using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ChartsFilter : EntityBase<Guid>
{
    public Guid ChartId { get; set; }

    public Guid PropertyId { get; set; }

    public string Value { get; set; } = null!;

    public int Order { get; set; }

    public int? OperatorId { get; set; }

    public virtual Chart Chart { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<ChartsFilter>
    {
        public void Configure(EntityTypeBuilder<ChartsFilter> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ChartsFilters_Id");
            builder.Property(e => e.Value).IsUnicode(false);

            builder.HasOne(d => d.Chart).WithMany(p => p.ChartsFilters)
                .HasForeignKey(d => d.ChartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChartsFilters_Charts");

            builder.HasOne(d => d.Property).WithMany(p => p.ChartsFilters)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChartsFilters_Properties");
        }
    }
}
