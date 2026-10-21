using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Role : EntityBase<Guid>, IHasCreated, IHasDeletedBy, IHasLastModifiedBy
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime Created { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; }

    public Guid? RoleLead { get; set; }

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<RolesAssetType> RolesAssetTypes { get; set; } = new List<RolesAssetType>();

    public virtual ICollection<RolesAssetTypesFilter> RolesAssetTypesFilters { get; set; } = new List<RolesAssetTypesFilter>();

    public virtual ICollection<RolesAssetTypesPermission> RolesAssetTypesPermissions { get; set; } = new List<RolesAssetTypesPermission>();

    public virtual ICollection<RolesAssetTypesProcess> RolesAssetTypesProcesses { get; set; } = new List<RolesAssetTypesProcess>();

    public virtual ICollection<RolesMember> RolesMembers { get; set; } = new List<RolesMember>();

    public virtual ICollection<RolesReport> RolesReports { get; set; } = new List<RolesReport>();

    public virtual ICollection<RolesView> RolesViews { get; set; } = new List<RolesView>();

    public class EntityConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Roles_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.IsActive).HasDefaultValue(true, "DF_Roles_IsActive");
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
        }
    }
}
