using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Chart : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy, IHasLastModified
{
    public string Name { get; set; } = null!;

    public string? PaletteName { get; set; }

    public string? AppearanceName { get; set; }

    public string? ChartTitle { get; set; }

    public string? LegendText { get; set; }

    public bool LegendVisible { get; set; }

    public string? SeriesName { get; set; }

    public string? ViewType { get; set; }

    public Guid AssetTypeId { get; set; }

    public byte[]? ChartImage { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public string? Title { get; set; }

    public string? Subtitle { get; set; }

    public bool IsPublic { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool? IsInstance { get; set; }

    public int? RecordCount { get; set; }

    public string? FilterExpression { get; set; }

    public string? GroupSql { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime? LastExecuted { get; set; }

    public string? FullSql { get; set; }

    public virtual ICollection<ChartsFilter> ChartsFilters { get; set; } = new List<ChartsFilter>();

    public virtual ICollection<ChartsProperty> ChartsProperties { get; set; } = new List<ChartsProperty>();

    public class EntityConfiguration : IEntityTypeConfiguration<Chart>
    {
        public void Configure(EntityTypeBuilder<Chart> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.AppearanceName)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.ChartTitle)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_Charts_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.FilterExpression)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.FullSql).IsUnicode(false);
            builder.Property(e => e.GroupSql).IsUnicode(false);
            builder.Property(e => e.LastExecuted).HasColumnType("smalldatetime");
            builder.Property(e => e.LastModified).HasColumnType("smalldatetime");
            builder.Property(e => e.LegendText)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.PaletteName)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.SeriesName)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Subtitle)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.Title)
                .HasMaxLength(100)
                .IsUnicode(false);
            builder.Property(e => e.ViewType)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
