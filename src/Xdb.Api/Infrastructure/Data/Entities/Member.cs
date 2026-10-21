using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Member : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public string UserId { get; set; } = null!;

    public string? Lname { get; set; }

    public string? Fname { get; set; }

    public string? Mname { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public bool? AllowBeta { get; set; }

    public virtual ICollection<MemberVisit> MemberVisits { get; set; } = new List<MemberVisit>();

    public virtual ICollection<RolesMember> RolesMembers { get; set; } = new List<RolesMember>();

    public class EntityConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_RoleMembers_1");

            builder.HasIndex(e => e.UserId, "uc_Unique_Members").IsUnique();

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Members_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Fname)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("FName");
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Lname)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LName");
            builder.Property(e => e.Mname)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MName");
            builder.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
