using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Files;

/// <summary>A stored file as a stream: bytes, MIME type ("" when the type has no MimeTypes row) and file name.</summary>
public sealed record StoredFile(byte[] Data, string MimeType, string Name);

public interface IFileQueryHandler
{
    /// <summary>getImageStream: the image (or else document) bytes; null when neither has data.</summary>
    Task<Result<StoredFile?>> GetStreamAsync(Guid imageId, bool createThumbnail, int maxSize, CancellationToken cancellationToken = default);
    Task<Result<string>> GetDimensionsAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task<Result<string>> GetUploadStatusAsync(string? id, CancellationToken cancellationToken = default);
}

/// <summary>The image and document reads (spec M8-F section 1).</summary>
public sealed class FileQueryHandler(IFileProcedures files, IMetadataProcedures metadata, UploadProgress progress) : IFileQueryHandler
{
    public async Task<Result<StoredFile?>> GetStreamAsync(Guid imageId, bool createThumbnail, int maxSize, CancellationToken cancellationToken = default)
    {
        if (await files.GetImageAsync(imageId, cancellationToken) is { Data: { } data } image)
        {
            if (createThumbnail)
                throw DeferredDependency.Thumbnail();
            return new Success<StoredFile?>(new StoredFile(data, await files.GetMimeTypeAsync(LegacyFileNames.ImageTypeName(image.ImageTypeId), cancellationToken), image.Name));
        }
        if (await files.GetDocumentAsync(imageId, cancellationToken) is not { Data: { } documentData } document)
            return new Success<StoredFile?>(null);
        return new Success<StoredFile?>(new StoredFile(documentData,
            await files.GetMimeTypeAsync(LegacyFileNames.DocumentTypeName(document.DocumentTypeId), cancellationToken), document.Name));
    }

    /// <summary>Image_GetDimensions: "WxH" (NULL sides as 0), "" for no live image.</summary>
    public async Task<Result<string>> GetDimensionsAsync(Guid imageId, CancellationToken cancellationToken = default) =>
        new Success<string>(await metadata.GetImageAsync(imageId, cancellationToken) is { } image ? $"{image.Width ?? 0}x{image.Height ?? 0}" : string.Empty);

    /// <summary>getUploadStatus: the byte count of an upload in this process; an unknown or null id faults (the original's catch threw).</summary>
    public Task<Result<string>> GetUploadStatusAsync(string? id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Result<string>>(new Success<string>(progress.Get(id).ToString(System.Globalization.CultureInfo.InvariantCulture)));
}

/// <summary>
/// Behaviour of the original that needed a Windows-only component (GDI+ through System.Drawing). Deferred by owner
/// decision 2026-10-23: the port fails at the same step instead (spec M8-F section 4), which matches the original
/// wherever it failed there too.
/// </summary>
public static class DeferredDependency
{
    /// <summary>D1: image width and height came from System.Drawing.Image.FromStream.</summary>
    public static Exception ImageDecode() => new NotSupportedException("Deferred: image decoding needs a Windows-only dependency (D1).");

    /// <summary>D2: thumbnails were resized and re-encoded with GDI+.</summary>
    public static Exception Thumbnail() => new NotSupportedException("Deferred: thumbnails need a Windows-only dependency (D2).");
}

public interface IUploadCommandHandler
{
    Task<Result<string>> UploadImageAsync(byte[] body, Guid imageId, Guid userId, string? fileName, Guid propertyId, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadDocumentAsync(byte[] body, Guid documentId, Guid userId, string? fileName, Guid propertyId, CancellationToken cancellationToken = default);
    Task<Result<bool>> PrepareAsync(Guid propertyId, Guid objectId, string? title, string? description, string? fileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Image_Upload, UploadDocument and prepareUploadObjectPropertyValue (spec M8-F section 2). No transactions; every
/// failure is the host fault of the operation (the uploads were XML-format operations: the request-error page).
/// </summary>
public sealed class UploadCommandHandler(IFileProcedures files, IMetadataProcedures metadata, UploadProgress progress, CurrentMember currentMember,
    IDateTimeProvider clock) : IUploadCommandHandler
{
    private DateTime Now() => clock.UtcNow().ToLocalTime();

    /// <summary>GetUploadData: the counter is set before reading and to the body length after, even when the body then fails.</summary>
    private byte[] Receive(byte[] body, string key)
    {
        progress.Set(key, 0);
        progress.Set(key, body.Length);
        return LegacyMultipart.GetFile(body);
    }

    public Task<Result<string>> UploadImageAsync(byte[] body, Guid imageId, Guid userId, string? fileName, Guid propertyId, CancellationToken cancellationToken = default)
    {
        _ = Receive(body, imageId.ToString());
        _ = LegacyFileNames.ImageType(LegacyFileNames.Name(fileName));
        // The original measured the image with GDI+ here, before validating or saving anything.
        throw DeferredDependency.ImageDecode();
    }

    public async Task<Result<string>> UploadDocumentAsync(byte[] body, Guid documentId, Guid userId, string? fileName, Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var key = documentId.ToString();
        var data = Receive(body, key);
        var name = LegacyFileNames.Name(fileName);
        var title = LegacyFileNames.Title(fileName);
        var created = Now();
        var type = LegacyFileNames.DocumentType(name);
        await ValidateDocumentAsync(documentId, name, title, userId, cancellationToken);
        await files.SaveDocumentAsync(new DocumentSave(documentId, type, name!, title, null, data, created, created, userId, userId), cancellationToken);
        return new Success<string>(LegacyUploadJson.Success(name, data.Length, propertyId, key));
    }

    /// <summary>
    /// prepareUploadObjectPropertyValue: a data-less document or image row for a later upload with the same id
    /// (approved when the current member is an administrator); false for a property that is neither.
    /// </summary>
    public async Task<Result<bool>> PrepareAsync(Guid propertyId, Guid objectId, string? title, string? description, string? fileName,
        CancellationToken cancellationToken = default)
    {
        var current = await currentMember.GetIdAsync(cancellationToken);
        var isAdmin = await metadata.IsAdminAsync(current, SystemRoleIds.Admin, cancellationToken);
        if (await metadata.GetPropertyAsync(propertyId, cancellationToken) is not { } property)
            return new Success<bool>(false);
        if (property.DataTypeId == (int)LegacyDataType.Document)
        {
            var name = LegacyFileNames.Name(LegacyFileNames.FormatNameTitle(title));
            var documentTitle = LegacyFileNames.Title(LegacyFileNames.FormatNameTitle(title));
            var created = Now();
            DateTime? approved = isAdmin ? Now() : null;
            var type = LegacyFileNames.DocumentType(fileName);
            await ValidateDocumentAsync(objectId, name, documentTitle, current, cancellationToken);
            await files.SaveDocumentAsync(new DocumentSave(objectId, type, name!, documentTitle, description, null, created, approved, current,
                isAdmin ? current : null), cancellationToken);
            return new Success<bool>(true);
        }
        if (property.DataTypeId == (int)LegacyDataType.Image)
        {
            var name = LegacyFileNames.Name(LegacyFileNames.FormatNameTitle(title));
            var created = Now();
            DateTime? approved = isAdmin ? Now() : null;
            var type = LegacyFileNames.ImageType(fileName);
            await ValidateImageAsync(type, name, current, cancellationToken);
            await files.SaveImageAsync(new ImageSave(objectId, type, name!, description, false, null, 0, 0, created, approved, current, isAdmin ? current : null),
                cancellationToken);
            return new Success<bool>(true);
        }
        return new Success<bool>(false);
    }

    private async Task ValidateDocumentAsync(Guid id, string? name, string? title, Guid createdBy, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (string.IsNullOrEmpty(name))
            throw new LegacyLogicalException("Name must be defined");
        if (string.IsNullOrEmpty(title))
            throw new LegacyLogicalException("Title must be defined");
        if (createdBy == Guid.Empty)
            throw new LegacyLogicalException("Created By cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(createdBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
    }

    private async Task ValidateImageAsync(int type, string? name, Guid createdBy, CancellationToken cancellationToken)
    {
        if (type == -1)
            throw new LegacyLogicalException("ImageType cannot be undefined");
        if (string.IsNullOrEmpty(name))
            throw new LegacyLogicalException("Name cannot be undefined");
        if (createdBy == Guid.Empty)
            throw new LegacyLogicalException("CreateBy cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(createdBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
    }
}
