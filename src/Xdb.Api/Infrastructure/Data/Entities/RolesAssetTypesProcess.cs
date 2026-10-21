using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesAssetTypesProcess : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid RoleId { get; set; }

    public Guid? AssetTypeId { get; set; }

    public Guid ProcessId { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual AssetType? AssetType { get; set; }

    public virtual Process Process { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesAssetTypesProcess>
    {
        public void Configure(EntityTypeBuilder<RolesAssetTypesProcess> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_RolesProcesses");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.AssetType).WithMany(p => p.RolesAssetTypesProcesses)
                .HasForeignKey(d => d.AssetTypeId)
                .HasConstraintName("FK_RolesAssetTypesProcesses_AssetTypes");

            builder.HasOne(d => d.Process).WithMany(p => p.RolesAssetTypesProcesses)
                .HasForeignKey(d => d.ProcessId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesProcesses_Processes");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesAssetTypesProcesses)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesAssetTypesProcesses_Roles");
        }
    }
}
