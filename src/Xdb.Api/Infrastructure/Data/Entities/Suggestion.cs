using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Suggestion : EntityBase<Guid>, IHasCreatedBy
{
    public string? Title { get; set; }

    public string Suggestion1 { get; set; } = null!;

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Suggestion>
    {
        public void Configure(EntityTypeBuilder<Suggestion> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Suggestions_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Suggestions_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Suggestion1)
                .IsUnicode(false)
                .HasColumnName("Suggestion");
            builder.Property(e => e.Title)
                .HasMaxLength(100)
                .IsUnicode(false);
        }
    }
}
