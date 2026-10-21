using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class QuestionGroup : EntityBase<Guid>, IHasCreated
{
    public Guid SurveyId { get; set; }

    public Guid AssetId { get; set; }

    public string Caption { get; set; } = null!;

    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Notified { get; set; }

    public DateTime? Closed { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<QuestionGroup>
    {
        public void Configure(EntityTypeBuilder<QuestionGroup> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Caption)
                .HasMaxLength(200)
                .IsUnicode(false);
            builder.Property(e => e.Closed).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_QuestionGroups_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Notified).HasColumnType("smalldatetime");
        }
    }
}
