using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class ViewsTrigger : EntityBase<Guid>
{
    public Guid ViewId { get; set; }

    public Guid? TriggerId { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<ViewsTrigger>
    {
        public void Configure(EntityTypeBuilder<ViewsTrigger> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_ViewTriggers");

            builder.HasIndex(e => e.ViewId, "IX_ViewIds");

            builder.Property(e => e.Id).ValueGeneratedNever();
        }
    }
}
