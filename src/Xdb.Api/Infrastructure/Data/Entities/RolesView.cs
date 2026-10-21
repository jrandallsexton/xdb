using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesView : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid RoleId { get; set; }

    public Guid ViewId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool IsAutoApprove { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual View View { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesView>
    {
        public void Configure(EntityTypeBuilder<RolesView> builder)
        {
            builder.HasIndex(e => new { e.RoleId, e.ViewId, e.Deleted }, "uc_Unique_Roles_Views").IsUnique();

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_RolesViews_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_RolesViews_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesViews)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesViews_Roles");

            builder.HasOne(d => d.View).WithMany(p => p.RolesViews)
                .HasForeignKey(d => d.ViewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesViews_Views");
        }
    }
}
