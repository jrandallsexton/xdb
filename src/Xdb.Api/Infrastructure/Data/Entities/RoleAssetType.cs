using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RoleAssetType : EntityBase<Guid>
{
    public Guid RoleId { get; set; }

    public Guid AssetTypeId { get; set; }

    public bool IsInstance { get; set; }

    public bool IsInherited { get; set; }

    public Guid? InheritFrom { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<RoleAssetType>
    {
        public void Configure(EntityTypeBuilder<RoleAssetType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
        }
    }
}
