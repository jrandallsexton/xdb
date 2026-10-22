using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Reports.Legacy;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Reports.Queries;

public sealed record GetReportQuery(Guid ReportId);

public interface IGetReportQueryHandler
{
    Task<Result<ReportContract>> ExecuteAsync(GetReportQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Report_Get: the loaded report (an unknown one fails).</summary>
public sealed class GetReportQueryHandler(ReportLoader loader) : IGetReportQueryHandler
{
    public async Task<Result<ReportContract>> ExecuteAsync(GetReportQuery query, CancellationToken cancellationToken = default) =>
        new Success<ReportContract>((await loader.LoadAsync(query.ReportId, cancellationToken)).Report);
}

public sealed record GetReportFilterStringQuery(Guid ReportId);

public interface IGetReportFilterStringQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetReportFilterStringQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetReportFilterStringQueryHandler(ReportLoader loader, ReportFilterString filterString) : IGetReportFilterStringQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetReportFilterStringQuery query, CancellationToken cancellationToken = default) =>
        new Success<string>(await filterString.BuildAsync((await loader.LoadAsync(query.ReportId, cancellationToken)).Report, cancellationToken));
}

public sealed record GetReportGroupColumnsQuery(Guid ReportId);

public interface IGetReportGroupColumnsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetReportGroupColumnsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ReportGroupColumns_Get: the grouping properties' display values.</summary>
public sealed class GetReportGroupColumnsQueryHandler(ReportLoader loader, IReportProcedures reports) : IGetReportGroupColumnsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetReportGroupColumnsQuery query, CancellationToken cancellationToken = default)
    {
        var report = (await loader.LoadAsync(query.ReportId, cancellationToken)).Report;
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var column in report.GroupColumns!)
            result.Add(new KeyValuePair<Guid, string>(column, await reports.GetPropertyDisplayValueAsync(column, cancellationToken)));
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}

public sealed record GetUnusedReportPropertiesQuery(Guid ReportId);

public interface IGetUnusedReportPropertiesQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUnusedReportPropertiesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Report_GetUnusedAvailableProperties: the type's properties not yet in the report; used Asset and User properties
/// (other than Instance Of) stay available for sub-property drill-down.
/// </summary>
public sealed class GetUnusedReportPropertiesQueryHandler(ReportLoader loader, MetadataRules rules, IMetadataProcedures metadata) : IGetUnusedReportPropertiesQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUnusedReportPropertiesQuery query, CancellationToken cancellationToken = default)
    {
        var loaded = await loader.LoadAsync(query.ReportId, cancellationToken);
        var report = loaded.Report;
        var requestType = report.IsInstance ? AssetRequestType.Instance : AssetRequestType.Definition;
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var (id, name) in await rules.PropertyDictionaryAsync(report.AssetTypeId, requestType, true, true, true, report.IsInstance, false, cancellationToken))
        {
            if (loaded.PropertyIds.Contains(id))
            {
                var property = await metadata.GetPropertyAsync(id, cancellationToken) ?? throw new NullReferenceException("Property not found.");
                if (property.SystemTypeId != (int)SystemType.InstanceOf && property.DataTypeId is (int)LegacyDataType.Asset or (int)LegacyDataType.User)
                    result.Add(new KeyValuePair<Guid, string>(id, name));
            }
            else
            {
                result.Add(new KeyValuePair<Guid, string>(id, name));
            }
        }
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}

public sealed record GetReportsDictionaryQuery(Guid UserId);

public interface IGetReportsDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetReportsDictionaryQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Reports_Get / Reports_GetForBuilder: every live report for an administrator, else the user's role reports.</summary>
public sealed class GetReportsDictionaryQueryHandler(IReportStore store, IMetadataProcedures metadata) : IGetReportsDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetReportsDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await store.GetReportsDictionaryAsync(
            await metadata.IsAdminAsync(query.UserId, SystemRoleIds.Admin, cancellationToken) ? null : query.UserId, cancellationToken));
}

public sealed record GetReportCacheQuery;

public interface IGetReportCacheQueryHandler
{
    Task<Result<List<ReportCacheEntryContract>>> ExecuteAsync(GetReportCacheQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ReportCache_Get: every report with its cache time and age (when cached) and hit count.</summary>
public sealed class GetReportCacheQueryHandler(IReportStore store, IDateTimeProvider dateTimeProvider) : IGetReportCacheQueryHandler
{
    private const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";

    public async Task<Result<List<ReportCacheEntryContract>>> ExecuteAsync(GetReportCacheQuery query, CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow().ToLocalTime();
        var result = new List<ReportCacheEntryContract>();
        foreach (var row in await store.GetCacheAsync(cancellationToken))
        {
            var entry = new ReportCacheEntryContract { ReportId = row.ReportId, ReportName = row.ReportName };
            if (row.CachedAt is { } cached)
            {
                entry.CacheTimestamp = cached.ToString(EnUsGeneralPattern, CultureInfo.InvariantCulture);
                var age = now - cached;
                entry.CacheAge = age.Days > 0 ? $"{age.Days} d, {age.Hours} hr, {age.Minutes} min, {age.Seconds} sec"
                    : age.Hours > 0 ? $"{age.Hours} hr, {age.Minutes} min, {age.Seconds} sec"
                    : $"{age.Minutes} min, {age.Seconds} sec";
            }
            if (row.Hits is { } hits)
                entry.HitCount = int.Parse(hits, CultureInfo.InvariantCulture);
            result.Add(entry);
        }
        return new Success<List<ReportCacheEntryContract>>(result);
    }
}

public sealed record CloneReportQuery(Guid ReportId, Guid UserId);

public interface ICloneReportQueryHandler
{
    Task<Result<ReportContract>> ExecuteAsync(CloneReportQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Report_Clone: "Copy of" the report with new ids for it, its filters and its properties (labels as loaded,
/// grouping and time-elapsed format not copied, no procedure).
/// </summary>
public sealed class CloneReportQueryHandler(ReportLoader loader, IDateTimeProvider dateTimeProvider) : ICloneReportQueryHandler
{
    public async Task<Result<ReportContract>> ExecuteAsync(CloneReportQuery query, CancellationToken cancellationToken = default)
    {
        var source = (await loader.LoadAsync(query.ReportId, cancellationToken)).Report;
        return new Success<ReportContract>(new ReportContract
        {
            Id = Guid.NewGuid(),
            ReportName = $"Copy of {source.ReportName}",
            Created = dateTimeProvider.UtcNow().ToLocalTime(),
            CreatedBy = query.UserId,
            AssetTypeId = source.AssetTypeId,
            FilterLogic = source.FilterLogic,
            Filters = [.. source.Filters!.Select(f => new FilterContract
            {
                Id = Guid.NewGuid(), DisplayValue = f.DisplayValue, Operator = f.Operator, OperatorId = f.OperatorId, Order = f.Order, Property = f.Property,
                PropertyId = f.PropertyId, Value = f.Value
            })],
            ReportProperties = [.. source.ReportProperties!.Select(p => new ReportPropertyContract
            {
                Id = Guid.NewGuid(), CustomReportFieldType = p.CustomReportFieldType, Index = p.Index, Label = p.Label, Logic = p.Logic, PropertyId = p.PropertyId,
                PropertyIds = p.PropertyIds is null ? null : [.. p.PropertyIds], PropertyName = p.PropertyName, SubPropertyId = p.SubPropertyId,
                SubPropertyName = p.SubPropertyName, TimeElapsedSpecificDate = p.TimeElapsedSpecificDate, TimeElapsedType = p.TimeElapsedType
            })],
            IsInstance = source.IsInstance,
            IsNew = true
        });
    }
}
