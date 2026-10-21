using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class SqlLog : EntityBase<int>, IHasCreated
{
    public string? InvokingClass { get; set; }

    public string? InvokingMethod { get; set; }

    public int? InvokingLineNo { get; set; }

    public string? InvokedClass { get; set; }

    public string? InvokedMethod { get; set; }

    public int? InvokedLineNo { get; set; }

    public string? SqlText { get; set; }

    public string? ParamList { get; set; }

    public DateTime Created { get; set; }

    public TimeOnly? TsStart { get; set; }

    public TimeOnly? TsEnd { get; set; }

    public string? StackTrace { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<SqlLog>
    {
        public void Configure(EntityTypeBuilder<SqlLog> builder)
        {
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_SqlLogs_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.InvokedClass)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.InvokedMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.InvokingClass)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.InvokingMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.ParamList).HasColumnType("text");
            builder.Property(e => e.SqlText).HasColumnType("text");
            builder.Property(e => e.StackTrace).IsUnicode(false);
            builder.Property(e => e.TsStart).HasDefaultValueSql("(getdate())", "DF_SqlLogs_TS");
        }
    }
}
