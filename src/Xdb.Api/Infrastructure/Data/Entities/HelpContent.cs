using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class HelpContent : EntityBase<Guid>
{
    public string Title { get; set; } = null!;

    public Guid ImageId { get; set; }

    public int Order { get; set; }

    public string Content { get; set; } = null!;

    public virtual Image Image { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<HelpContent>
    {
        public void Configure(EntityTypeBuilder<HelpContent> builder)
        {
            builder.ToTable("HelpContent");

            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Content).IsUnicode(false);
            builder.Property(e => e.Title)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.HasOne(d => d.Image).WithMany(p => p.HelpContents)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HelpContent_Images");
        }
    }
}
