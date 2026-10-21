using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ProcessStepFilter : EntityBase<Guid>
{
    public Guid ProcessStepId { get; set; }

    public Guid PropertyId { get; set; }

    public int Order { get; set; }

    public virtual ProcessStep ProcessStep { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<ProcessStepFilter>
    {
        public void Configure(EntityTypeBuilder<ProcessStepFilter> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasOne(d => d.ProcessStep).WithMany(p => p.ProcessStepFilters)
                .HasForeignKey(d => d.ProcessStepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcessStepFilters_ProcessSteps");

            builder.HasOne(d => d.Property).WithMany(p => p.ProcessStepFilters)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcessStepFilters_Properties");
        }
    }
}
