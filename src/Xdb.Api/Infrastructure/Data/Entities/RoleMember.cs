using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RoleMember : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid RoleId { get; set; }

    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<RoleMember>
    {
        public void Configure(EntityTypeBuilder<RoleMember> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_RoleMembers_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_RoleMembers_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
        }
    }
}
