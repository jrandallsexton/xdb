using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesAssetTypesFilter : EntityBase<Guid>
{
    public Guid RoleId { get; set; }

    public Guid AssetTypeId { get; set; }

    public bool IsIncludesFilter { get; set; }

    public int Order { get; set; }

    public Guid? LogicalOperatorId { get; set; }

    public Guid OperatorId { get; set; }

    public Guid PropertyId { get; set; }

    public string Value { get; set; } = null!;

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual Operator Operator { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesAssetTypesFilter>
    {
        public void Configure(EntityTypeBuilder<RolesAssetTypesFilter> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_RolesFilters");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.LogicalOperatorId).HasColumnName("LogicalOperatorID");
            builder.Property(e => e.Value).IsUnicode(false);

            builder.HasOne(d => d.AssetType).WithMany(p => p.RolesAssetTypesFilters)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesFilters_AssetTypes");

            builder.HasOne(d => d.Operator).WithMany(p => p.RolesAssetTypesFilters)
                .HasForeignKey(d => d.OperatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesFilters_Operators");

            builder.HasOne(d => d.Property).WithMany(p => p.RolesAssetTypesFilters)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesFilters_Properties");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesAssetTypesFilters)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesFilters_Roles");
        }
    }
}
