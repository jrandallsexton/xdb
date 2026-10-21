using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggerLogsMember : EntityBase<Guid>, IHasCreated
{
    public Guid TriggerLogId { get; set; }

    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Viewed { get; set; }

    public DateTime? Read { get; set; }

    public DateTime? Pinned { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<TriggerLogsMember>
    {
        public void Configure(EntityTypeBuilder<TriggerLogsMember> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_TriggerLogsMembers_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_TriggerLogsMembers_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Pinned).HasColumnType("smalldatetime");
            builder.Property(e => e.Read).HasColumnType("smalldatetime");
            builder.Property(e => e.Viewed).HasColumnType("smalldatetime");
        }
    }
}
