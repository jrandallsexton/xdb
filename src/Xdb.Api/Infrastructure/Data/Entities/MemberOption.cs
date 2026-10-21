using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MemberOption : EntityBase<Guid>
{
    public Guid MemberId { get; set; }

    public Guid OptionTypeId { get; set; }

    public string OptionValue { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<MemberOption>
    {
        public void Configure(EntityTypeBuilder<MemberOption> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.OptionValue)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
