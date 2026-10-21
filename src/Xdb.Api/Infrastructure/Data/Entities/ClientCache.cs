using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ClientCache : EntityBase<int>, IHasCreated
{
    public string? Key { get; set; }

    public string? Value { get; set; }

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ClientCache>
    {
        public void Configure(EntityTypeBuilder<ClientCache> builder)
        {
            builder.ToTable("ClientCache");

            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_ClientCache_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Key)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Value).IsUnicode(false);
        }
    }
}
