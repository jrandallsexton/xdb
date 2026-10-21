using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolePermission : EntityBase<Guid>
{
    public Guid RoleAssetTypesId { get; set; }

    public int PermissionId { get; set; }

    public bool IsIncludesFilter { get; set; }

    public string? FilterLogic { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_RolePermissions_1");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.FilterLogic).IsUnicode(false);
        }
    }
}
