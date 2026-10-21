using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RoleFilter : EntityBase<Guid>
{
    public Guid RolePermissionsId { get; set; }

    public int Order { get; set; }

    public Guid PropertyId { get; set; }

    public int OperatorId { get; set; }

    public string? Value { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<RoleFilter>
    {
        public void Configure(EntityTypeBuilder<RoleFilter> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false);
        }
    }
}
