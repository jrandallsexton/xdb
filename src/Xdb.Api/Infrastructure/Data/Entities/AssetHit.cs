using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetHit : EntityBase<int>, IHasCreated
{
    public Guid AssetId { get; set; }

    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<AssetHit>
    {
        public void Configure(EntityTypeBuilder<AssetHit> builder)
        {
            builder.HasIndex(e => e.MemberId, "IX_MemberIds");

            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AssetHits_Created")
                .HasColumnType("smalldatetime");

            builder.HasOne(d => d.Asset).WithMany(p => p.AssetHits)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetHits_Assets");
        }
    }
}
