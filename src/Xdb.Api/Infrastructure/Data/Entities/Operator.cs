using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Operator : EntityBase<Guid>
{
    public string Operator1 { get; set; } = null!;

    public byte Order { get; set; }

    public virtual ICollection<RolesAssetTypesFilter> RolesAssetTypesFilters { get; set; } = new List<RolesAssetTypesFilter>();

    public virtual ICollection<ViewFilter> ViewFilters { get; set; } = new List<ViewFilter>();

    public class EntityConfiguration : IEntityTypeConfiguration<Operator>
    {
        public void Configure(EntityTypeBuilder<Operator> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Operator1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Operator");
        }
    }
}
