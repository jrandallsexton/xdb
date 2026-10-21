using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class BulkUploadTemplatesProperty : EntityBase<Guid>
{
    public Guid BulkUploadTemplateId { get; set; }

    public Guid PropertyId { get; set; }

    public int Order { get; set; }

    public class EntityConfiguration : IEntityTypeConfiguration<BulkUploadTemplatesProperty>
    {
        public void Configure(EntityTypeBuilder<BulkUploadTemplatesProperty> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
        }
    }
}
