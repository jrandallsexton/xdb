using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class SystemOption
{
    public bool? LogDynamicSql { get; set; }

    public bool? LogStoredProcs { get; set; }

    public int? ReportCache { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<SystemOption>
    {
        public void Configure(EntityTypeBuilder<SystemOption> builder)
        {
            builder.HasNoKey();
        }
    }
}
