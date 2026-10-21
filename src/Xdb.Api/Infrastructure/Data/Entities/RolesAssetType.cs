using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesAssetType : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid RoleId { get; set; }

    public Guid AssetTypeId { get; set; }

    public bool InheritAccess { get; set; }

    public string? FilterLogic { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool? IsInstance { get; set; }

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesAssetType>
    {
        public void Configure(EntityTypeBuilder<RolesAssetType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.FilterLogic).IsUnicode(false);
            builder.Property(e => e.InheritAccess).HasDefaultValue(true, "DF_RolesAssetTypes_InheritAccess");

            builder.HasOne(d => d.AssetType).WithMany(p => p.RolesAssetTypes)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypes_AssetTypes");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesAssetTypes)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypes_Roles");
        }
    }
}
