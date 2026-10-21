using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ServiceModule : EntityBase<Guid>, IHasCreated, ISoftDeletable
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? AssemblyName { get; set; }

    public string? AssemblyNamespace { get; set; }

    public string? ClassName { get; set; }

    public byte[]? AssemblyData { get; set; }

    public string? AssemblyVersion { get; set; }

    public string? IsActive { get; set; }

    public DateTime? Downloaded { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Deleted { get; set; }

    public DateTime? LastRan { get; set; }

    public DateTime? LastCompleted { get; set; }

    public DateTime? LastPing { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ServiceModule>
    {
        public void Configure(EntityTypeBuilder<ServiceModule> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.AssemblyName)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.AssemblyNamespace).IsUnicode(false);
            builder.Property(e => e.AssemblyVersion)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.ClassName)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.Downloaded).HasColumnType("smalldatetime");
            builder.Property(e => e.IsActive)
                .HasMaxLength(10)
                .IsFixedLength();
            builder.Property(e => e.LastCompleted).HasColumnType("smalldatetime");
            builder.Property(e => e.LastPing).HasColumnType("smalldatetime");
            builder.Property(e => e.LastRan).HasColumnType("smalldatetime");
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
