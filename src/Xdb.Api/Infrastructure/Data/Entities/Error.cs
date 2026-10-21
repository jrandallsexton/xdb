using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Error : EntityBase<Guid>, IHasCreated
{
    public string Message { get; set; } = null!;

    public string? Exception { get; set; }

    public Guid? MemberId { get; set; }

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Error>
    {
        public void Configure(EntityTypeBuilder<Error> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Errors_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Errors_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Exception).IsUnicode(false);
            builder.Property(e => e.Message).IsUnicode(false);
        }
    }
}
