using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AssetClient : EntityBase<int>, IHasCreated
{
    public Guid AssetId { get; set; }

    public Guid ViewId { get; set; }

    public Guid UserId { get; set; }

    public byte[] Data { get; set; } = null!;

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<AssetClient>
    {
        public void Configure(EntityTypeBuilder<AssetClient> builder)
        {
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF__AssetClie__Creat__7E208022")
                .HasColumnType("smalldatetime");
        }
    }
}
