using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Trigger : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public int? TypeId { get; set; }

    public string? FilterExpression { get; set; }

    public virtual ICollection<TriggersAssetType> TriggersAssetTypes { get; set; } = new List<TriggersAssetType>();

    public virtual ICollection<TriggersAsset> TriggersAssets { get; set; } = new List<TriggersAsset>();

    public virtual ICollection<TriggersMember> TriggersMembers { get; set; } = new List<TriggersMember>();

    public virtual ICollection<TriggersProperty> TriggersProperties { get; set; } = new List<TriggersProperty>();

    public virtual ICollection<TriggersRestriction> TriggersRestrictions { get; set; } = new List<TriggersRestriction>();

    public virtual ICollection<TriggersRole> TriggersRoles { get; set; } = new List<TriggersRole>();

    public virtual ICollection<TriggersTargetProperty> TriggersTargetProperties { get; set; } = new List<TriggersTargetProperty>();

    public class EntityConfiguration : IEntityTypeConfiguration<Trigger>
    {
        public void Configure(EntityTypeBuilder<Trigger> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.FilterExpression).IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
