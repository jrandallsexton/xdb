using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggersAssetType : EntityBase<Guid>
{
    public Guid TriggerId { get; set; }

    public Guid AssetTypeId { get; set; }

    public bool IncludesChildren { get; set; }

    public virtual Trigger Trigger { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<TriggersAssetType>
    {
        public void Configure(EntityTypeBuilder<TriggersAssetType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasOne(d => d.Trigger).WithMany(p => p.TriggersAssetTypes)
                .HasForeignKey(d => d.TriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TriggersAssetTypes_Triggers");
        }
    }
}
