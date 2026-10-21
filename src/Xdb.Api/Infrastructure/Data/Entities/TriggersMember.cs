using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersMember : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid MemberId { get; set; }

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersMember>
    {
        public void Configure(EntityTypeBuilder<TriggersMember> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_TriggersMembers_Id");

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersMembers)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersMembers_Triggers");
        }
    }
}
