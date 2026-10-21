using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Question : EntityBase<Guid>, IHasCreated
{
    public Guid QuestionGroupId { get; set; }

    public int Index { get; set; }

    public bool AllowUnknown { get; set; }

    public bool DisableNext { get; set; }

    public string? DisableCondition { get; set; }

    public Guid MemberId { get; set; }

    public Guid? PropertyValueId { get; set; }

    public string Message { get; set; } = null!;

    public string? ResponseText { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Responded { get; set; }

    public bool? Response { get; set; }

    public Guid? PropertyId { get; set; }

    public string? UserProvidedValue { get; set; }

    public bool? UserProvidedValueIsValid { get; set; }

    public Guid? UserProvidedValueAssetId { get; set; }

    public DateTime? Processed { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Questions_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Questions_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.DisableCondition)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Message).IsUnicode(false);
            builder.Property(e => e.Processed).HasColumnType("smalldatetime");
            builder.Property(e => e.Responded).HasColumnType("smalldatetime");
            builder.Property(e => e.ResponseText).IsUnicode(false);
            builder.Property(e => e.UserProvidedValue).IsUnicode(false);
        }
    }
}
