using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ProcessStep : EntityBase<Guid>
{
    public Guid ProcessId { get; set; }

    public int Order { get; set; }

    public string Name { get; set; } = null!;

    public Guid? ViewId { get; set; }

    public bool IsInstance { get; set; }

    public bool IsCreation { get; set; }

    public bool ShowAssetTypeSelector { get; set; }

    public string? AssetNameLabel { get; set; }

    public bool IsEdit { get; set; }

    public virtual Process Process { get; set; } = null!;

    public virtual ICollection<ProcessStepFilter> ProcessStepFilters { get; set; } = new List<ProcessStepFilter>();

    public class EntityConfiguration : IEntityTypeConfiguration<ProcessStep>
    {
        public void Configure(EntityTypeBuilder<ProcessStep> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_ProcessSteps_Id");
            builder.Property(e => e.AssetNameLabel)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.HasOne(d => d.Process).WithMany(p => p.ProcessSteps)
                .HasForeignKey(d => d.ProcessId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProcessSteps_Processes");
        }
    }
}
