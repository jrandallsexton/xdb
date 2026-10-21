using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ViewFilter : EntityBase<Guid>
{
    public Guid ViewId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid OperatorId { get; set; }

    public string Value { get; set; } = null!;

    public int Order { get; set; }

    public virtual Operator Operator { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public virtual View View { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<ViewFilter>
    {
        public void Configure(EntityTypeBuilder<ViewFilter> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Value).IsUnicode(false);

            builder.HasOne(d => d.Operator).WithMany(p => p.ViewFilters)
                .HasForeignKey(d => d.OperatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewFilters_Operators");

            builder.HasOne(d => d.Property).WithMany(p => p.ViewFilters)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewFilters_Properties");

            builder.HasOne(d => d.View).WithMany(p => p.ViewFilters)
                .HasForeignKey(d => d.ViewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewFilters_Views");
        }
    }
}
