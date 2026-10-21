using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersRole : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid RoleId { get; set; }

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersRole>
    {
        public void Configure(EntityTypeBuilder<TriggersRole> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_TriggersRoles_Id");

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersRoles)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersRoles_Triggers");
        }
    }
}
