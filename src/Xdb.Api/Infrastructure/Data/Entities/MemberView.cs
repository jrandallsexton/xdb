using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MemberView : EntityBase<Guid>
{
    public Guid MemberId { get; set; }

    public Guid ViewId { get; set; }

    public string Name { get; set; } = null!;

    public string FilterExpression { get; set; } = null!;

    public string? FilterString { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<MemberView>
    {
        public void Configure(EntityTypeBuilder<MemberView> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_MemberViews_Id");
            builder.Property(e => e.FilterExpression).IsUnicode(false);
            builder.Property(e => e.FilterString).IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
        }
    }
}
