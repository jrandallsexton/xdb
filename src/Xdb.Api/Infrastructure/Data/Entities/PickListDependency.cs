using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PickListDependency : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid ParentPickListId { get; set; }

    public Guid ParentPickListValueId { get; set; }

    public Guid ChildPickListId { get; set; }

    public Guid ChildPickListValueId { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual PickList ChildPickList { get; set; } = null!;

    public virtual PickListValue ChildPickListValue { get; set; } = null!;

    public virtual PickList ParentPickList { get; set; } = null!;

    public virtual PickListValue ParentPickListValue { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<PickListDependency>
    {
        public void Configure(EntityTypeBuilder<PickListDependency> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_PickListDependencies_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");

            builder.HasOne(d => d.ChildPickList).WithMany(p => p.PickListDependencyChildPickLists)
                .HasForeignKey(d => d.ChildPickListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PickListDependencies_PickLists1");

            builder.HasOne(d => d.ChildPickListValue).WithMany(p => p.PickListDependencyChildPickListValues)
                .HasForeignKey(d => d.ChildPickListValueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PickListDependencies_PickListValues1");

            builder.HasOne(d => d.ParentPickList).WithMany(p => p.PickListDependencyParentPickLists)
                .HasForeignKey(d => d.ParentPickListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PickListDependencies_PickLists");

            builder.HasOne(d => d.ParentPickListValue).WithMany(p => p.PickListDependencyParentPickListValues)
                .HasForeignKey(d => d.ParentPickListValueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PickListDependencies_PickListValues");
        }
    }
}
