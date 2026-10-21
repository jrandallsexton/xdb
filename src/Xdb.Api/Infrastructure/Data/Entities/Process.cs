using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Process : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public string Name { get; set; } = null!;

    public DateTime Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid? DeletedBy { get; set; }

    public string? DisplayValue { get; set; }

    public Guid? AssetTypeId { get; set; }

    public virtual ICollection<ProcessStep> ProcessSteps { get; set; } = new List<ProcessStep>();

    public virtual ICollection<RolesAssetTypesProcess> RolesAssetTypesProcesses { get; set; } = new List<RolesAssetTypesProcess>();

    public class EntityConfiguration : IEntityTypeConfiguration<Process>
    {
        public void Configure(EntityTypeBuilder<Process> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_Processes_Id");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Processes_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.DisplayValue).IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
