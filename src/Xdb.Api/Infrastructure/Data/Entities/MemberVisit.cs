using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MemberVisit : EntityBase<Guid>, IHasCreated
{
    public Guid MemberId { get; set; }

    public DateTime Created { get; set; }

    public string? BrowserName { get; set; }

    public string? BrowserVersion { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual Member Member { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<MemberVisit>
    {
        public void Configure(EntityTypeBuilder<MemberVisit> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_MemberVisits_Id");
            builder.Property(e => e.BrowserName)
                .HasMaxLength(10)
                .IsUnicode(false);
            builder.Property(e => e.BrowserVersion)
                .HasMaxLength(10)
                .IsUnicode(false);
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_MemberVisits_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.UserAgent)
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.HasOne(d => d.Member).WithMany(p => p.MemberVisits)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MemberVisits_Members");
        }
    }
}
