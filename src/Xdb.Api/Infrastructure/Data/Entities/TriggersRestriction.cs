using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersRestriction : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RestrictedPropertyId { get; set; }

    public Guid? SourcePropertyId { get; set; }

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersRestriction>
    {
        public void Configure(EntityTypeBuilder<TriggersRestriction> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersRestrictions)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersRestrictions_Triggers");
        }
    }
}
