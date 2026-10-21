using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Dashboard : EntityBase<Guid>, IHasCreated, ISoftDeletable
{
    public string Caption { get; set; } = null!;

    public int Index { get; set; }

    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Dashboard>
    {
        public void Configure(EntityTypeBuilder<Dashboard> builder)
        {
            builder.HasKey(e => e.Id).HasFillFactor(80);

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Dashboards_Id");
            builder.Property(e => e.Caption)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Dashboards_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
        }
    }
}
