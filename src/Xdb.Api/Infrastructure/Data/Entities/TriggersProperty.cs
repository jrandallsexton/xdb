using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersProperty : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid? OperatorId { get; set; }

    public string? Threshold { get; set; }

    public string? Value { get; set; }

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersProperty>
    {
        public void Configure(EntityTypeBuilder<TriggersProperty> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Threshold)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Value).IsUnicode(false);

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersProperties)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersProperties_Triggers");
        }
    }
}
