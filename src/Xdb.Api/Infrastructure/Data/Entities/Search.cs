using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Search : EntityBase<int>, IHasCreatedBy
{
    public string SearchPhrase { get; set; } = null!;

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Search>
    {
        public void Configure(EntityTypeBuilder<Search> builder)
        {
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Searches_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.SearchPhrase).IsUnicode(false);
        }
    }
}
