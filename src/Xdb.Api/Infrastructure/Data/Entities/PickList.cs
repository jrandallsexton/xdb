using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PickList : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModifiedBy
{
    public string Name { get; set; } = null!;

    public string? DisplayValue { get; set; }

    public string? Description { get; set; }

    public bool IsMemberList { get; set; }

    public bool AllowNewValues { get; set; }

    public string? ServiceUrl { get; set; }

    public string? ServiceMethod { get; set; }

    public string? ServiceUsername { get; set; }

    public string? ServicePassword { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? LastModifiedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public virtual ICollection<PickListDependency> PickListDependencyChildPickLists { get; set; } = new List<PickListDependency>();

    public virtual ICollection<PickListDependency> PickListDependencyParentPickLists { get; set; } = new List<PickListDependency>();

    public virtual ICollection<PickListValue> PickListValues { get; set; } = new List<PickListValue>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public class EntityConfiguration : IEntityTypeConfiguration<PickList>
    {
        public void Configure(EntityTypeBuilder<PickList> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_PickLists_uiId");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_PickLists_dtCreated")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.DisplayValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.ServiceMethod)
                .HasMaxLength(255)
                .IsUnicode(false);
            builder.Property(e => e.ServicePassword)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.ServiceUrl).IsUnicode(false);
            builder.Property(e => e.ServiceUsername)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
