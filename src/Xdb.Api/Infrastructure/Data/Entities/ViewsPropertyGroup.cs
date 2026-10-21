using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ViewsPropertyGroup : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid ViewId { get; set; }

    public Guid PropertyGroupId { get; set; }

    public int Order { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual PropertyGroup PropertyGroup { get; set; } = null!;

    public virtual View View { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<ViewsPropertyGroup>
    {
        public void Configure(EntityTypeBuilder<ViewsPropertyGroup> builder)
        {
            builder.HasIndex(e => e.ViewId, "IX_ViewIds");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ViewsPropertyGroups_Id");
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.PropertyGroup).WithMany(p => p.ViewsPropertyGroups)
                .HasForeignKey(d => d.PropertyGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewsCategories_Categories");

            builder.HasOne(d => d.View).WithMany(p => p.ViewsPropertyGroups)
                .HasForeignKey(d => d.ViewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ViewsCategories_Views");
        }
    }
}
