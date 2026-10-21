using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class PropertyGroupsProperty : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public Guid PropertyGroupId { get; set; }

    public Guid PropertyId { get; set; }

    public int Order { get; set; }

    public bool IsRequired { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool? IsReadOnly { get; set; }

    public string? DefaultValue { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual PropertyGroup PropertyGroup { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<PropertyGroupsProperty>
    {
        public void Configure(EntityTypeBuilder<PropertyGroupsProperty> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.DefaultValue).IsUnicode(false);
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.IsReadOnly).HasDefaultValue(false, "DF_PropertyGroupsProperties_IsReadOnly");

            builder.HasOne(d => d.PropertyGroup).WithMany(p => p.PropertyGroupsProperties)
                .HasForeignKey(d => d.PropertyGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoriesProperties_Categories");

            builder.HasOne(d => d.Property).WithMany(p => p.PropertyGroupsProperties)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoriesProperties_Properties");
        }
    }
}
