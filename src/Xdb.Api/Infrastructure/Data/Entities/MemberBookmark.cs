using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class MemberBookmark : EntityBase<Guid>
{
    public Guid MemberId { get; set; }

    public Guid? ViewId { get; set; }

    public Guid? AssetId { get; set; }

    public Guid? ReportId { get; set; }

    public bool ShowOnHome { get; set; }

    public virtual Report? Report { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<MemberBookmark>
    {
        public void Configure(EntityTypeBuilder<MemberBookmark> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasOne(d => d.Report).WithMany(p => p.MemberBookmarks)
                .HasForeignKey(d => d.ReportId)
                .HasConstraintName("FK_MemberBookmarks_Reports");
        }
    }
}
