using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MemberSetting : EntityBase<int>, IHasCreated
{
    public Guid MemberId { get; set; }

    public string Key { get; set; } = null!;

    public string? Value { get; set; }

    public DateTime Created { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<MemberSetting>
    {
        public void Configure(EntityTypeBuilder<MemberSetting> builder)
        {
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_MemberSettings_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Key)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Value)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
