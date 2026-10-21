using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class AgentReport : EntityBase<Guid>, IHasCreated
{
    public Guid? AssetId { get; set; }

    public DateTime Created { get; set; }

    public byte[] Report { get; set; } = null!;

    public string ReportVersion { get; set; } = null!;

    public DateTime? Processed { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<AgentReport>
    {
        public void Configure(EntityTypeBuilder<AgentReport> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_AgentReports_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AgentReports_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Processed).HasColumnType("smalldatetime");
            builder.Property(e => e.ReportVersion)
                .HasMaxLength(10)
                .IsUnicode(false);
        }
    }
}
