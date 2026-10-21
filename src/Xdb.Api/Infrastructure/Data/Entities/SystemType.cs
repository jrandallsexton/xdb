using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class SystemType : EntityBase<int>
{
    public string SystemType1 { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public class EntityConfiguration : IEntityTypeConfiguration<SystemType>
    {
        public void Configure(EntityTypeBuilder<SystemType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.SystemType1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SystemType");
        }
    }
}
