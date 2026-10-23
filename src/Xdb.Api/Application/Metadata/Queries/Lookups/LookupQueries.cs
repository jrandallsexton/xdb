using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Metadata.Queries.Lookups;

public sealed record GetPropertyDataTypeQuery(Guid PropertyId);

public interface IGetPropertyDataTypeQueryHandler
{
    Task<Result<int>> ExecuteAsync(GetPropertyDataTypeQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Property_GetDataType: the loaded property's data type number (-1 when unknown). A missing property fails.</summary>
public sealed class GetPropertyDataTypeQueryHandler(IMetadataProcedures procedures) : IGetPropertyDataTypeQueryHandler
{
    public async Task<Result<int>> ExecuteAsync(GetPropertyDataTypeQuery query, CancellationToken cancellationToken = default)
    {
        var row = await procedures.GetPropertyAsync(query.PropertyId, cancellationToken)
            ?? throw new NullReferenceException("Property not found.");
        return new Success<int>(MetadataLoader.Property(row).DataType);
    }
}

public sealed record GetOperatorsQuery;

public interface IGetOperatorsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<int, string>>>> ExecuteAsync(GetOperatorsQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetOperatorsQueryHandler(IMetadataProcedures procedures) : IGetOperatorsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<int, string>>>> ExecuteAsync(GetOperatorsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<int, string>>>(await procedures.GetOperatorsAsync(cancellationToken));
}

public sealed record GetPermissionsQuery;

public interface IGetPermissionsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<int, string>>>> ExecuteAsync(GetPermissionsQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPermissionsQueryHandler(IMetadataProcedures procedures) : IGetPermissionsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<int, string>>>> ExecuteAsync(GetPermissionsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<int, string>>>(await procedures.GetPermissionsAsync(cancellationToken));
}

public sealed record GetCurrencySymbolsQuery;

public interface IGetCurrencySymbolsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetCurrencySymbolsQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetCurrencySymbolsQueryHandler(IMetadataProcedures procedures) : IGetCurrencySymbolsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetCurrencySymbolsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetCurrencySymbolsAsync(cancellationToken));
}

public sealed record GetCurrencyValueQuery(Guid Id);

public interface IGetCurrencyValueQueryHandler
{
    Task<Result<CurrencyValueContract?>> ExecuteAsync(GetCurrencyValueQuery query, CancellationToken cancellationToken = default);
}

/// <summary>An amount with its symbol (the symbol text cast to nvarchar(30) by the original's SQL); null when absent.</summary>
public sealed class GetCurrencyValueQueryHandler(IMetadataProcedures procedures) : IGetCurrencyValueQueryHandler
{
    public async Task<Result<CurrencyValueContract?>> ExecuteAsync(GetCurrencyValueQuery query, CancellationToken cancellationToken = default)
    {
        var row = await procedures.GetCurrencyValueAsync(query.Id, cancellationToken);
        return new Success<CurrencyValueContract?>(row is null ? null : new CurrencyValueContract
        {
            Id = query.Id,
            Amount = row.Value,
            SymbolId = row.CurrencySymbolId,
            SymbolAscii = row.Symbol,
            SymbolText = row.SymbolDisplay
        });
    }
}

public sealed record GetSystemImageDictionaryQuery;

public interface IGetSystemImageDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetSystemImageDictionaryQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetSystemImageDictionaryQueryHandler(IMetadataProcedures procedures) : IGetSystemImageDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetSystemImageDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetSystemImageDictionaryAsync(cancellationToken));
}

/// <summary>Configuration for environment-bound legacy values (section "LegacyEnvironment").</summary>
public sealed class LegacyEnvironmentOptions
{
    public const string SectionName = "LegacyEnvironment";

    /// <summary>The original service's help page address, linked from its wrong-verb (405) page.</summary>
    public string ServiceHelpUrl { get; set; } = string.Empty;

    /// <summary>
    /// The original service's own address (its restSvcAddress setting, no trailing slash), used to build image
    /// stream URLs. Environment-specific; set it locally, never commit it.
    /// </summary>
    public string RestServiceAddress { get; set; } = string.Empty;

    /// <summary>The web server URL (the original web server URL setting) that related-item links start with.</summary>
    public string WebServerUrl { get; set; } = string.Empty;

    /// <summary>The web application's virtual directory (virtualDir), combined with <see cref="WebServerUrl"/>.</summary>
    public string VirtualDir { get; set; } = string.Empty;

    /// <summary>The client UI root (clientUiRoot) that recent-item view URLs start with.</summary>
    public string ClientUiRoot { get; set; } = string.Empty;

    /// <summary>The client's view page name (clientViewPageName).</summary>
    public string ClientViewPageName { get; set; } = string.Empty;

    /// <summary>
    /// The member RGraph reads related items as (the original hard-coded one; whether it is an administrator decides
    /// whether role joins apply). Environment data; set it locally, never commit it.
    /// </summary>
    public Guid RGraphMemberId { get; set; }

    /// <summary>
    /// The account name the original resolved its caller to (anonymous requests ran as the application pool's
    /// account), mapped to a member by Members.UserId. Environment data; set it locally, never commit it.
    /// </summary>
    public string CurrentUserName { get; set; } = string.Empty;

    /// <summary>
    /// The original's own application instance number (its applicationInstance setting; DEV = "1"), the only
    /// MigrateReport target that reached a database the port can see.
    /// </summary>
    public string ApplicationInstance { get; set; } = "1";

    /// <summary>The chart subtitle the original hard-coded on new charts ("Source: " and the product name). Set it locally.</summary>
    public string ChartSubtitle { get; set; } = string.Empty;

    /// <summary>
    /// The original's full type name of its asset request type enum, which its serializer quoted when a wrapped body's
    /// requestType could not be read. Environment data; set it locally, never commit it.
    /// </summary>
    public string AssetRequestTypeName { get; set; } = string.Empty;

    /// <summary>
    /// The original's web server URL helper: the server URL combined with the virtual directory by .NET Framework Path.Combine
    /// rules (a rooted second part replaces the first; a first part ending in a separator or ':' is concatenated;
    /// otherwise joined with '\').
    /// </summary>
    public string CombinedWebServerUrl()
    {
        if (string.IsNullOrEmpty(VirtualDir))
            return WebServerUrl;
        if (VirtualDir[0] is '\\' or '/' || (VirtualDir.Length > 1 && VirtualDir[1] == ':'))
            return VirtualDir;
        if (WebServerUrl.Length == 0)
            return VirtualDir;
        return WebServerUrl[^1] is '\\' or '/' or ':' ? WebServerUrl + VirtualDir : WebServerUrl + "\\" + VirtualDir;
    }
}

public sealed record GetSystemImagesQuery;

public interface IGetSystemImagesQueryHandler
{
    Task<Result<List<ImageContract>>> ExecuteAsync(GetSystemImagesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// System images by name, each with its stream URL. IsSystem is always false (never read); a deleted system image
/// loads as null and the original then failed.
/// </summary>
public sealed class GetSystemImagesQueryHandler(IMetadataProcedures procedures, IOptions<LegacyEnvironmentOptions> options) : IGetSystemImagesQueryHandler
{
    public async Task<Result<List<ImageContract>>> ExecuteAsync(GetSystemImagesQuery query, CancellationToken cancellationToken = default)
    {
        var prefix = $"{options.Value.RestServiceAddress}/getImageStream?imageId=";
        var images = new List<ImageContract>();
        foreach (var id in await procedures.GetSystemImageIdsAsync(cancellationToken))
        {
            var row = await procedures.GetImageAsync(id, cancellationToken)
                ?? throw new NullReferenceException("System image not found.");
            images.Add(new ImageContract
            {
                Id = id,
                ImageType = row.ImageTypeId,
                Order = row.Order,
                Name = row.Name ?? string.Empty,
                Description = row.Description ?? string.Empty,
                Width = row.Width ?? 0,
                Height = row.Height ?? 0,
                Created = row.Created,
                Approved = row.Approved,
                CreatedBy = row.CreatedBy,
                ApprovedBy = row.ApprovedBy,
                DeletedBy = row.DeletedBy,
                RequestUrl = prefix + id.ToString("D")
            });
        }
        return new Success<List<ImageContract>>(images);
    }
}

public sealed record GenerateGuidQuery(int DataType);

public interface IGenerateGuidQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(GenerateGuidQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A new Guid; for Asset and User, one that is not a live asset or member id.</summary>
public sealed class GenerateGuidQueryHandler(IMetadataProcedures procedures) : IGenerateGuidQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(GenerateGuidQuery query, CancellationToken cancellationToken = default)
    {
        var guid = Guid.NewGuid();
        switch (query.DataType)
        {
            case (int)DataType.Asset:
                while (await procedures.IsLiveAssetIdAsync(guid, cancellationToken))
                    guid = Guid.NewGuid();
                break;
            case (int)DataType.User:
                while (await procedures.IsLiveMemberIdAsync(guid, cancellationToken))
                    guid = Guid.NewGuid();
                break;
        }
        return new Success<Guid>(guid);
    }
}
