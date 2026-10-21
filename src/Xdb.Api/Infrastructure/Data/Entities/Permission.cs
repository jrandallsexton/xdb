using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Permission : EntityBase<Guid>, IHasCreated
{
    public int Value { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayValue { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime Created { get; set; }

    public Guid? AssetTypeId { get; set; }

    public byte? AssetRequestType { get; set; }

    public virtual ICollection<RolesAssetTypesPermission> RolesAssetTypesPermissions { get; set; } = new List<RolesAssetTypesPermission>();

    public class EntityConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Permissions_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Permissions_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.DisplayValue).IsUnicode(false);
            builder.Property(e => e.Name).IsUnicode(false);
        }
    }
}
