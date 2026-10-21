using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class TriggerLog : IHasCreated
{
    public Guid Id { get; set; }

    public Guid TriggerId { get; set; }

    public Guid AssetId { get; set; }

    public Guid SubmittalId { get; set; }

    public Guid PropertyValueId { get; set; }

    public Guid PropertyId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string TriggerText { get; set; } = null!;

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<TriggerLog>
    {
        public void Configure(EntityTypeBuilder<TriggerLog> builder)
        {
            builder.HasNoKey();

            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_TriggerLogs_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_TriggerLogs_Id");
            builder.Property(e => e.NewValue).IsUnicode(false);
            builder.Property(e => e.OldValue).IsUnicode(false);
            builder.Property(e => e.TriggerText).IsUnicode(false);
        }
    }
}
