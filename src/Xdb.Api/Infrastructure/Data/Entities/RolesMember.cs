using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesMember : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid MemberId { get; set; }

    public Guid RoleId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesMember>
    {
        public void Configure(EntityTypeBuilder<RolesMember> builder)
        {
            builder.HasIndex(e => e.MemberId, "IX_MemberIds");

            builder.HasIndex(e => e.RoleId, "IX_RoleIds");

            builder.HasIndex(e => new { e.MemberId, e.RoleId, e.Deleted }, "uc_Unique_Members_InRole").IsUnique();

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_RolesMembers_Id");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_RolesMembers_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Member).WithMany(p => p.RolesMembers)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesMembers_Members");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesMembers_Roles");
        }
    }
}
