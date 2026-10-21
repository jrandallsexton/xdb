using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersAsset : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid AssetId { get; set; }

    public bool IncludesInstances { get; set; }

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersAsset>
    {
        public void Configure(EntityTypeBuilder<TriggersAsset> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersAssets)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersAssets_Triggers");
        }
    }
}
