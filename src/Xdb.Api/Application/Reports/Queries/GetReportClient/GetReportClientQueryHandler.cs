using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation.Results;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Reports.Queries.GetReportClient.Dtos;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Reports.Queries.GetReportClient;

public interface IGetReportClientQueryHandler
{
    Task<Result<ReportClientDto>> ExecuteAsync(GetReportClientQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Report metadata for the viewer, reproducing the original's naming and filter-rendering rules exactly.
/// Like the original it reads deleted reports too, and data problems (a report without properties, a filter value
/// that is not a valid id) surface as exceptions.
/// </summary>
public sealed class GetReportClientQueryHandler(IReportProcedures procedures) : IGetReportClientQueryHandler
{
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    public async Task<Result<ReportClientDto>> ExecuteAsync(GetReportClientQuery query, CancellationToken cancellationToken = default)
    {
        var (report, properties) = await procedures.GetReportAsync(query.ReportId, cancellationToken);
        if (report is null)
            return new Failure<ReportClientDto>(default!, ResultStatus.NotFound, [new ValidationFailure(nameof(query.ReportId), "Report not found")]);

        var names = string.IsNullOrEmpty(report.StoredProc)
            ? await PropertyNamesAsync(report, properties, cancellationToken)
            : await StoredProcedureColumnNamesAsync(report.StoredProc, cancellationToken);

        var groupColumns = properties.Where(p => p.IsGrouping && p.PropertyId.HasValue).Select(p => p.PropertyId!.Value).Distinct().ToList();
        string? groupColumnName = null;
        if (groupColumns.Count > 0)
            groupColumnName = await procedures.GetPropertyDisplayValueAsync(groupColumns[^1], cancellationToken);

        var filterString = await FilterStringAsync(query.ReportId, report, cancellationToken);

        return new Success<ReportClientDto>(new ReportClientDto(
            report.ReportName,
            names,
            groupColumns.Count > 0 ? groupColumns : null,
            groupColumnName,
            filterString));
    }

    private async Task<IReadOnlyList<string?>> PropertyNamesAsync(ReportRow report, IReadOnlyList<ReportPropertyRow> properties, CancellationToken cancellationToken)
    {
        var dictionary = await PropertiesAsync(properties.Select(p => p.PropertyId), cancellationToken);
        var names = new List<string?>();
        foreach (var rp in properties)
            names.Add(await PropertyNameAsync(report, rp, dictionary, cancellationToken));
        return names;
    }

    /// <summary>
    /// A stored-procedure report's headers are the procedure's result columns, without its row number and asset id.
    /// The procedure is executed to find them; null when it fails.
    /// </summary>
    private async Task<IReadOnlyList<string?>?> StoredProcedureColumnNamesAsync(string storedProcedure, CancellationToken cancellationToken)
    {
        var table = await procedures.ExecuteReportProcedureAsync(storedProcedure, cancellationToken);
        return table?.Columns.Cast<DataColumn>()
            .Select(c => c.ColumnName)
            .Where(name => name != "rn" && name != "AssetId")
            .ToList<string?>();
    }

    /// <summary>No ids, no lookup: the procedure itself fails on an empty list.</summary>
    private async Task<Dictionary<Guid, PropertyRow>> PropertiesAsync(IEnumerable<Guid?> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0)
            return [];
        return (await procedures.GetPropertiesAsync(JoinIds(distinct), cancellationToken)).ToDictionary(p => p.Id);
    }

    private async Task<string?> PropertyNameAsync(ReportRow report, ReportPropertyRow rp, IReadOnlyDictionary<Guid, PropertyRow> dictionary, CancellationToken cancellationToken)
    {
        var property = rp.PropertyId is { } id ? dictionary[id] : throw new KeyNotFoundException("Report property without a property id.");

        if (rp.SubPropertyId.HasValue)
            return string.IsNullOrEmpty(rp.Label) ? $"{rp.Property ?? string.Empty}.{rp.SubProperty ?? string.Empty}" : rp.Label;

        if (rp.CustomReportFieldTypeId is { } custom && custom != 0)
            return rp.Label;

        if (property.IsSystem)
        {
            switch (ToSystemType(property.SystemTypeId))
            {
                case SystemType.AssetName:
                    var label = await procedures.GetReportingLabelAsync(report.AssetTypeId, forInstances: report.IsInstance, cancellationToken);
                    return string.IsNullOrEmpty(label) ? "Asset Name" : label;
                case SystemType.InstanceOf:
                    return await procedures.GetReportingLabelAsync(report.AssetTypeId, forInstances: !report.IsInstance, cancellationToken);
                case SystemType.InstanceOfDescription:
                    return await procedures.GetReportingLabelAsync(report.AssetTypeId, forInstances: !report.IsInstance, cancellationToken) + " Description";
            }
        }

        return string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue;
    }

    private async Task<string> FilterStringAsync(Guid reportId, ReportRow report, CancellationToken cancellationToken)
    {
        var filters = await procedures.GetReportFiltersAsync(reportId, cancellationToken);
        if (filters.Count == 0)
            return string.Empty;

        var dictionary = await PropertiesAsync(filters.Select(f => (Guid?)f.PropertyId), cancellationToken);
        var items = new List<object>();
        foreach (var filter in filters)
        {
            var property = dictionary[filter.PropertyId];
            var propertyName = await procedures.GetPropertyDisplayValueAsync(filter.PropertyId, cancellationToken);
            var propertyValue = filter.Value;

            switch ((DataType)property.DataTypeId)
            {
                case DataType.Date:
                    if (filter.Value != "NULL" && !property.IsSystem && Guid.TryParse(filter.Value, out var datePartId)
                        && await procedures.GetDatePartDisplayValueAsync(datePartId, cancellationToken) is { } datePart)
                        propertyValue = datePart;
                    break;

                case DataType.Asset:
                case DataType.RelationParentChild:
                case DataType.RelationChildParent:
                case DataType.RelationOther:
                    if (property.IsSystem && ToSystemType(property.SystemTypeId) is SystemType.AssetName or SystemType.InstanceOf)
                        propertyName = report.ReportingLabel!;
                    propertyValue = await procedures.GetAssetNameAsync(new Guid(filter.Value), cancellationToken);
                    break;

                case DataType.PickList:
                    var value = await procedures.GetPickListValueAsync(new Guid(filter.Value), cancellationToken)
                        ?? throw new InvalidOperationException($"Pick list value {filter.Value} not found.");
                    propertyValue = string.IsNullOrEmpty(value.DisplayValue) ? value.Value : value.DisplayValue;
                    break;

                case DataType.User:
                    propertyValue = await MemberDisplayValueAsync(new Guid(filter.Value), cancellationToken);
                    break;
            }

            if (filter.Value == EmptyGuid)
                propertyValue = "null";

            items.Add($"{propertyName} {filter.Operator} {propertyValue}");
        }

        // The stored filter expression is a composite format string over the rendered items, e.g. "(({0}) AND ({1}))".
        return string.Format(CultureInfo.InvariantCulture, report.FilterExpression ?? string.Empty, items.ToArray());
    }

    private async Task<string> MemberDisplayValueAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await procedures.GetMemberAsync(memberId, cancellationToken);
        if (member is null)
            return string.Empty;
        if (string.IsNullOrEmpty(member.FName) && string.IsNullOrEmpty(member.LName))
            return member.UserId;
        return $"{member.LName}, {member.FName} [{member.UserId}]";
    }

    private static SystemType ToSystemType(int? id) =>
        id is { } value && Enum.IsDefined(typeof(SystemType), value) ? (SystemType)value : SystemType.NotApplicable;

    /// <summary>Ids as the original passed them: lowercase "D" format, comma-separated, no spaces.</summary>
    private static string JoinIds(IEnumerable<Guid?> ids) =>
        string.Join(",", ids.Select(id => id?.ToString("D") ?? string.Empty));
}
