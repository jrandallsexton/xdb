using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersTargetProperty : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid PropertyId { get; set; }

    public string NewValue { get; set; } = null!;

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersTargetProperty>
    {
        public void Configure(EntityTypeBuilder<TriggersTargetProperty> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.NewValue).IsUnicode(false);

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersTargetProperties)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersTargetProperties_Triggers");
        }
    }
}
