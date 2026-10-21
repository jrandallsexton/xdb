using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Bug : EntityBase<Guid>, IHasCreatedBy
{
    public int Area { get; set; }

    public int Criticality { get; set; }

    public string? Title { get; set; }

    public string Bug1 { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Bug>
    {
        public void Configure(EntityTypeBuilder<Bug> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_MembersIssues");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Bugs_Id");
            builder.Property(e => e.Bug1)
                .IsUnicode(false)
                .HasColumnName("Bug");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_MembersIssues_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Title)
                .HasMaxLength(100)
                .IsUnicode(false);
        }
    }
}
