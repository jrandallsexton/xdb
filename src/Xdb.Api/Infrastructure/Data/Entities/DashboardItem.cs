using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class DashboardItem : EntityBase<Guid>, IHasCreated, ISoftDeletable
{
    public Guid DashboardId { get; set; }

    public int Index { get; set; }

    public byte ItemTypeId { get; set; }

    public Guid ChartId { get; set; }

    public string? Title { get; set; }

    public string? Subtitle { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<DashboardItem>
    {
        public void Configure(EntityTypeBuilder<DashboardItem> builder)
        {
            builder.HasKey(e => e.Id).HasFillFactor(80);

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_DashboardItems_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_DashboardItems_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Subtitle)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Title)
                .HasMaxLength(100)
                .IsUnicode(false);
        }
    }
}
