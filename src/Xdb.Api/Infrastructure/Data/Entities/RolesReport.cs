using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class RolesReport : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid RoleId { get; set; }

    public Guid ReportId { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual Report Report { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<RolesReport>
    {
        public void Configure(EntityTypeBuilder<RolesReport> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.Report).WithMany(p => p.RolesReports)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesReports_Reports");

            builder.HasOne(d => d.Role).WithMany(p => p.RolesReports)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesReports_Roles");
        }
    }
}
