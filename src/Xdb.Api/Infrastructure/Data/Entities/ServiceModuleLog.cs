using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ServiceModuleLog : EntityBase<Guid>, IHasCreated
{
    public Guid ServiceModuleId { get; set; }

    public string Category { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime Created { get; set; }

    public Guid? LogGroupId { get; set; }

    public int? Order { get; set; }

    public Guid? AssetId { get; set; }

    public Guid? MemberId { get; set; }

    public Guid? PropertyId { get; set; }

    public Guid? PickListId { get; set; }

    public Guid? PickListValueId { get; set; }

    public string? Subcategory { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ServiceModuleLog>
    {
        public void Configure(EntityTypeBuilder<ServiceModuleLog> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            builder.Property(e => e.Created).HasColumnType("smalldatetime");
            builder.Property(e => e.Message).IsUnicode(false);
            builder.Property(e => e.Subcategory)
                .HasMaxLength(50)
                .IsUnicode(false);
        }
    }
}
