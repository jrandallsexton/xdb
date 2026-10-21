using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Xdb.Api.Infrastructure.Data.Entities.Common;

namespace Xdb.Api.Infrastructure.Data.Entities;

public class Image : EntityBase<Guid>, IHasCreatedBy, IHasDeletedBy
{
    public byte ImageTypeId { get; set; }

    public byte Order { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public byte[]? ImageData { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Approved { get; set; }

    public DateTime? Deleted { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? ApprovedBy { get; set; }

    public Guid? DeletedBy { get; set; }

    public bool? IsSystem { get; set; }

    public virtual ICollection<AssetType> AssetTypes { get; set; } = new List<AssetType>();

    public virtual ICollection<HelpContent> HelpContents { get; set; } = new List<HelpContent>();

    public virtual ImageType ImageType { get; set; } = null!;

    public class EntityConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasKey(e => e.Id).HasName("PK_AssetImages");

            builder.Property(e => e.Id).HasDefaultValueSql("(newid())", "DF_AssetImages_Id");
            builder.Property(e => e.Approved).HasColumnType("smalldatetime");
            builder.Property(e => e.Created)
                .HasDefaultValueSql("(getdate())", "DF_AssetImages_Created")
                .HasColumnType("smalldatetime");
            builder.Property(e => e.Deleted).HasColumnType("smalldatetime");
            builder.Property(e => e.Description).IsUnicode(false);
            builder.Property(e => e.IsSystem).HasDefaultValue(false, "DF_Images_IsSystem");
            builder.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.HasOne(d => d.ImageType).WithMany(p => p.Images)
                .HasForeignKey(d => d.ImageTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Images_ImageTypes");
        }
    }
}
