using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MemberViewFilter
{
    public Guid Id { get; set; }

    public Guid MemberViewId { get; set; }

    public Guid PropertyId { get; set; }

    public int OperatorId { get; set; }

    public string? Value { get; set; }

    public int Order { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<MemberViewFilter>
    {
        public void Configure(EntityTypeBuilder<MemberViewFilter> builder)
        {
            builder.HasNoKey();

            builder.Property(e => e.Value).IsUnicode(false);
        }
    }
}
