using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A live image with its data (spr_Image_Get).</summary>
public sealed record ImageDataRow(int ImageTypeId, string Name, byte[]? Data);

/// <summary>A live document with its data (spr_Document_Get).</summary>
public sealed record DocumentDataRow(int DocumentTypeId, string Name, byte[]? Data);

/// <summary>An image as ImageDal.Save sends it (a null value is not supplied).</summary>
public sealed record ImageSave(Guid Id, int ImageTypeId, string Name, string? Description, bool IsSystem, byte[]? Data, int Width, int Height, DateTime Created,
    DateTime? Approved, Guid CreatedBy, Guid? ApprovedBy);

/// <summary>A document as DocumentDal.Save sends it (a null value is not supplied).</summary>
public sealed record DocumentSave(Guid Id, int DocumentTypeId, string Name, string? Title, string? Description, byte[]? Data, DateTime Created, DateTime? Approved,
    Guid CreatedBy, Guid? ApprovedBy);

/// <summary>The original's image and document data access (spec M8-F), with its SQL text.</summary>
public interface IFileProcedures
{
    Task<ImageDataRow?> GetImageAsync(Guid imageId, CancellationToken cancellationToken);
    Task<DocumentDataRow?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);
    /// <summary>MimeTypeDal.MimeType_Get: the first MimeTypes row for ".&lt;type name&gt;", trimmed; "" when none.</summary>
    Task<string> GetMimeTypeAsync(string typeName, CancellationToken cancellationToken);
    /// <summary>spr_Image_Save. An existing id goes through spr_Image_Update with two arguments swapped (kept: the original's bug).</summary>
    Task SaveImageAsync(ImageSave image, CancellationToken cancellationToken);
    Task SaveDocumentAsync(DocumentSave document, CancellationToken cancellationToken);
}
