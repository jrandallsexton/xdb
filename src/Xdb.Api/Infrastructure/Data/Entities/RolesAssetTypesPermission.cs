using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesAssetTypesPermission : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid RoleId { get; set; }

    public Guid AssetTypeId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesAssetTypesPermission>
    {
        public void Configure(EntityTypeBuilder<RolesAssetTypesPermission> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.AssetType).WithMany(p => p.RolesAssetTypesPermissions)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesPermissions_AssetTypes");

            builder.HasOne(d => d.Permission).WithMany(p => p.RolesAssetTypesPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesPermissions_Permissions");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesAssetTypesPermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesPermissions_Roles");
        }
    }
}
