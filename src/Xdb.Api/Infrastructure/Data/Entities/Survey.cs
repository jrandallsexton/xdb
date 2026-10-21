using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Survey : EntityBase<Guid>, IHasCreated
{
    public string Name { get; set; } = null!;

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Survey>
    {
        public void Configure(EntityTypeBuilder<Survey> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Surveys_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
        }
    }
}
