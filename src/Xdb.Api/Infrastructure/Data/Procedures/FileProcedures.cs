using System;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class FileProcedures(XdbDataContext db) : LegacyGatewayBase(db), IFileProcedures
{
    public async Task<ImageDataRow?> GetImageAsync(Guid imageId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Image_Get", new { Id = imageId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return new ImageDataRow(reader.GetByte(0), reader.GetString(2), reader.IsDBNull(4) ? null : (byte[])reader.GetValue(4));
    }

    public async Task<DocumentDataRow?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Document_Get", new { Id = documentId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return new DocumentDataRow(reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(4) ? null : (byte[])reader.GetValue(4));
    }

    public Task<string> GetMimeTypeAsync(string typeName, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [MimeType] FROM [MimeTypes] WHERE [FileExtension] = '." + typeName + "'", null, cancellationToken);

    public async Task SaveImageAsync(ImageSave image, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Image_Save", Supplied(
            ("Id", image.Id), ("ImageTypeId", image.ImageTypeId), ("Order", (byte)0), ("Name", image.Name), ("Description", image.Description ?? string.Empty),
            ("IsSystem", image.IsSystem), ("ImageData", image.Data), ("Width", image.Width), ("Height", image.Height), ("Created", image.Created),
            ("Approved", image.Approved), ("CreatedBy", image.CreatedBy), ("ApprovedBy", image.ApprovedBy)), cancellationToken));

    public async Task SaveDocumentAsync(DocumentSave document, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Document_Save", Supplied(
            ("Id", document.Id), ("DocumentTypeId", document.DocumentTypeId), ("Name", document.Name), ("Title", document.Title),
            ("Description", string.IsNullOrEmpty(document.Description) ? null : document.Description), ("Data", document.Data), ("Created", document.Created),
            ("Approved", document.Approved), ("CreatedBy", document.CreatedBy), ("ApprovedBy", document.ApprovedBy)), cancellationToken));
}
